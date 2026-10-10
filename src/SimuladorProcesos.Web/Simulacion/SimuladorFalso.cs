using SimuladorProcesos.Core;
using SimuladorProcesos.Core.Modelos;
using SimuladorProcesos.Engine;

namespace SimuladorProcesos.Web.Simulacion;

/// <summary>
/// ISimulador de mentira para construir la GUI sin esperar al motor: cada ThMs un temporizador copia un carácter.
/// No usa hilos por proceso ni estructuras propias; se reemplaza por el motor real en la subtarea 2.3.
/// </summary>
public sealed class SimuladorFalso : ISimulador
{
    private readonly object _candado = new();
    private readonly Timer _temporizador;
    private readonly List<ProcesoFalso> _listos = [];
    private readonly List<ProcesoFalso> _terminados = [];

    private IReadOnlyList<ProcesoEntrada> _entradas = [];
    private ConfiguracionSimulacion _configuracion = new(1, 1);
    private ProcesoFalso? _ejecucion;
    private long _relojMs;
    private int _caracteresEnTurno;
    private EstadoSimulacion _estado = EstadoSimulacion.Detenida;
    private bool _desechado;

    public SimuladorFalso()
    {
        _temporizador = new Timer(Avanzar, null, Timeout.Infinite, Timeout.Infinite);
    }

    public EstadoSimulacion Estado
    {
        get
        {
            lock (_candado)
            {
                return _estado;
            }
        }
    }

    public event EventHandler<CambioEstadoEventArgs>? CambioEstado;

    public event EventHandler? SimulacionFinalizada;

    public void Cargar(IReadOnlyList<ProcesoEntrada> procesos, ConfiguracionSimulacion configuracion)
    {
        ArgumentNullException.ThrowIfNull(procesos);
        ArgumentNullException.ThrowIfNull(configuracion);
        lock (_candado)
        {
            if (_estado is EstadoSimulacion.EnCurso or EstadoSimulacion.Pausada)
            {
                throw new InvalidOperationException("Solo se puede cargar en estado Detenida o Finalizada.");
            }

            _entradas = [.. procesos];
            _configuracion = configuracion;
            Preparar();
        }
    }

    public void Iniciar()
    {
        var eventos = new List<CambioEstadoEventArgs>();
        lock (_candado)
        {
            if (_estado != EstadoSimulacion.Detenida || _listos.Count == 0)
            {
                throw new InvalidOperationException("Iniciar requiere procesos cargados y estado Detenida.");
            }

            _estado = EstadoSimulacion.EnCurso;
            foreach (var proceso in _listos)
            {
                eventos.Add(new CambioEstadoEventArgs(proceso.Instantanea(), null, EstadoProceso.Listo, _relojMs));
            }

            _temporizador.Change(_configuracion.ThMs, _configuracion.ThMs);
        }

        Notificar(eventos, finalizo: false);
    }

    public void Pausar()
    {
        lock (_candado)
        {
            if (_estado != EstadoSimulacion.EnCurso)
            {
                return;
            }

            _estado = EstadoSimulacion.Pausada;
            _temporizador.Change(Timeout.Infinite, Timeout.Infinite);
        }
    }

    public void Reanudar()
    {
        lock (_candado)
        {
            if (_estado != EstadoSimulacion.Pausada)
            {
                return;
            }

            _estado = EstadoSimulacion.EnCurso;
            _temporizador.Change(_configuracion.ThMs, _configuracion.ThMs);
        }
    }

    public void Reiniciar()
    {
        lock (_candado)
        {
            _temporizador.Change(Timeout.Infinite, Timeout.Infinite);
            Preparar();
        }
    }

    public InstantaneaSimulacion ObtenerInstantanea()
    {
        lock (_candado)
        {
            return new InstantaneaSimulacion(
                _estado,
                _relojMs,
                [.. _listos.Select(p => p.Instantanea())],
                _ejecucion?.Instantanea(),
                [.. _terminados.Select(p => p.Instantanea())]);
        }
    }

    public IReadOnlyList<FilaReporte> ObtenerReporte()
    {
        lock (_candado)
        {
            return [.. _terminados.Select(Fila)];
        }
    }

    public void Dispose()
    {
        lock (_candado)
        {
            _desechado = true;
        }

        _temporizador.Dispose();
    }

    // Deja los procesos cargados en Listo y el reloj en cero; el llamador ya tiene el candado.
    private void Preparar()
    {
        _listos.Clear();
        _terminados.Clear();
        _ejecucion = null;
        _relojMs = 0;
        _caracteresEnTurno = 0;
        _estado = EstadoSimulacion.Detenida;

        long llegadaAnterior = 0;
        for (var i = 0; i < _entradas.Count; i++)
        {
            var entrada = _entradas[i];
            var llegada = Formulas.TiempoLlegada(listoVacio: i == 0, llegadaAnterior);
            llegadaAnterior = llegada;
            _listos.Add(new ProcesoFalso(i + 1, entrada, entrada.QuantumPropio ?? _configuracion.Quantum, llegada, _configuracion.ThMs));
        }
    }

    // Un tick del temporizador = un carácter copiado; los eventos se disparan fuera del candado, como pide ISimulador.
    private void Avanzar(object? estadoTemporizador)
    {
        var eventos = new List<CambioEstadoEventArgs>();
        var finalizo = false;
        lock (_candado)
        {
            if (_desechado || _estado != EstadoSimulacion.EnCurso)
            {
                return;
            }

            _relojMs += _configuracion.ThMs;

            if (_ejecucion is null && _listos.Count > 0)
            {
                _ejecucion = _listos[0];
                _listos.RemoveAt(0);
                _ejecucion.Estado = EstadoProceso.Ejecucion;
                _caracteresEnTurno = 0;
                eventos.Add(new CambioEstadoEventArgs(_ejecucion.Instantanea(), EstadoProceso.Listo, EstadoProceso.Ejecucion, _relojMs));
            }

            if (_ejecucion is { } actual)
            {
                actual.CaracteresRestantes--;
                _caracteresEnTurno++;

                if (actual.CaracteresRestantes == 0)
                {
                    actual.Ejecuciones++;
                    actual.Estado = EstadoProceso.Terminado;
                    actual.TiempoFinalizacionReal = _relojMs;
                    _terminados.Add(actual);
                    _ejecucion = null;
                    eventos.Add(new CambioEstadoEventArgs(actual.Instantanea(), EstadoProceso.Ejecucion, EstadoProceso.Terminado, _relojMs));
                }
                else if (actual.Entrada.Prioridad == Prioridad.Expulsivo && _caracteresEnTurno >= actual.Quantum)
                {
                    // Un no expulsivo nunca entra aquí: corre hasta terminar.
                    actual.Ejecuciones++;
                    actual.Estado = EstadoProceso.Listo;
                    _listos.Add(actual);
                    _ejecucion = null;
                    eventos.Add(new CambioEstadoEventArgs(actual.Instantanea(), EstadoProceso.Ejecucion, EstadoProceso.Listo, _relojMs));
                }
            }

            if (_ejecucion is null && _listos.Count == 0)
            {
                _estado = EstadoSimulacion.Finalizada;
                _temporizador.Change(Timeout.Infinite, Timeout.Infinite);
                finalizo = true;
            }
        }

        Notificar(eventos, finalizo);
    }

    private void Notificar(List<CambioEstadoEventArgs> eventos, bool finalizo)
    {
        foreach (var evento in eventos)
        {
            CambioEstado?.Invoke(this, evento);
        }

        if (finalizo)
        {
            SimulacionFinalizada?.Invoke(this, EventArgs.Empty);
        }
    }

    private static FilaReporte Fila(ProcesoFalso proceso)
    {
        var finReal = proceso.TiempoFinalizacionReal ?? 0;
        // T.R usa el TF real porque refleja la espera (CONTEXT §3, duda 4); T.F es el del enunciado.
        var turnaround = Formulas.Turnaround(finReal, proceso.TiempoLlegada);
        return new FilaReporte(
            proceso.Entrada.Nombre,
            proceso.TiempoLlegada,
            proceso.Rafaga,
            (int)proceso.Entrada.Prioridad,
            turnaround,
            Formulas.TiempoFinalizacionEnunciado(proceso.Quantum, proceso.Ejecuciones),
            finReal,
            Formulas.Espera(turnaround, proceso.Rafaga));
    }

    private sealed class ProcesoFalso(int numero, ProcesoEntrada entrada, int quantum, long tiempoLlegada, int thMs)
    {
        public int Numero { get; } = numero;

        public ProcesoEntrada Entrada { get; } = entrada;

        public int Quantum { get; } = quantum;

        public long TiempoLlegada { get; } = tiempoLlegada;

        public long Rafaga { get; } = Formulas.Rafaga(thMs, entrada.Descripcion.Length);

        public EstadoProceso Estado { get; set; } = EstadoProceso.Listo;

        public int Ejecuciones { get; set; }

        public int CaracteresRestantes { get; set; } = entrada.Descripcion.Length;

        public long? TiempoFinalizacionReal { get; set; }

        public ProcesoInstantanea Instantanea() => new(
            Numero,
            Entrada.Pid,
            Entrada.Nombre,
            Entrada.Prioridad,
            Estado,
            Quantum,
            Ejecuciones,
            Entrada.Descripcion.Length,
            CaracteresRestantes,
            Estado == EstadoProceso.Listo && Ejecuciones > 0,
            TiempoLlegada,
            Rafaga,
            TiempoFinalizacionReal);
    }
}
