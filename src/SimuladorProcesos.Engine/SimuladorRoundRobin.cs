using System.Diagnostics;
using SimuladorProcesos.Core;
using SimuladorProcesos.Engine.Actividad;
using SimuladorProcesos.Engine.Calculos;
using SimuladorProcesos.Engine.Modelo;
using SimuladorProcesos.Engine.Planificacion;

namespace SimuladorProcesos.Engine;

/// <summary>
/// M5: Round Robin con un hilo dispatcher y un Thread por proceso (ADR-0002) sobre el planificador sin hilos.
/// Solo un proceso recibe el permiso de turno a la vez, y el único lock protege al planificador.
/// </summary>
public sealed class SimuladorRoundRobin : ISimulador
{
    // Un hilo bloqueado en un handler o en el destino no debe colgar Reiniciar ni Dispose para siempre.
    private static readonly TimeSpan _plazoCierre = TimeSpan.FromSeconds(2);

    // Candado único del motor: protege al planificador, el estado de la simulación y la lista de avisos.
    private readonly object _estado = new();
    private readonly IDestinoActividad _destino;
    private readonly bool _poseeDestino;

    // El planificador avisa dentro del lock; los eventos se guardan aquí y se disparan fuera (ADR-0002).
    private readonly List<CambioEstadoEventArgs> _pendientes = [];

    private IReadOnlyList<ProcesoEntrada> _procesos = [];
    private ConfiguracionSimulacion _configuracion = new(thMs: 1, quantum: 1);
    private PlanificadorRoundRobin? _planificador;
    private Corrida? _corrida;
    private EstadoSimulacion _estadoSimulacion = EstadoSimulacion.Detenida;
    private bool _liberado;

    /// <param name="destino">Dónde escriben los procesos; por defecto, archivos en la carpeta de actividad de la Web.</param>
    public SimuladorRoundRobin(IDestinoActividad? destino = null)
    {
        _poseeDestino = destino is null;
        _destino = destino ?? new DestinoArchivos(Path.Combine(RutasSimulador.CarpetaActividad, "web"));
    }

    public event EventHandler<CambioEstadoEventArgs>? CambioEstado;

    public event EventHandler? SimulacionFinalizada;

    public EstadoSimulacion Estado
    {
        get
        {
            lock (_estado)
            {
                return _estadoSimulacion;
            }
        }
    }

    /// <summary>Hilos de la corrida actual (dispatcher y procesos); las pruebas comprueban con él que ninguno queda vivo.</summary>
    internal IReadOnlyList<Thread> Hilos
    {
        get
        {
            lock (_estado)
            {
                return _corrida?.Hilos.ToArray() ?? [];
            }
        }
    }

    public void Cargar(IReadOnlyList<ProcesoEntrada> procesos, ConfiguracionSimulacion configuracion)
    {
        ArgumentNullException.ThrowIfNull(procesos);
        ArgumentNullException.ThrowIfNull(configuracion);

        Corrida? anterior;
        lock (_estado)
        {
            ComprobarNoLiberado();
            if (_estadoSimulacion is not (EstadoSimulacion.Detenida or EstadoSimulacion.Finalizada))
            {
                throw new InvalidOperationException($"No se puede cargar con la simulación {_estadoSimulacion}: hay que reiniciarla primero.");
            }

            // Se copia la lista para que Reiniciar recree el planificador aunque el llamador la modifique después.
            var copia = procesos.ToArray();

            // Si el planificador rechaza los datos (quantum <= 0) no se ha tocado nada de lo cargado antes.
            _planificador = CrearPlanificador(copia, configuracion);
            _procesos = copia;
            _configuracion = configuracion;
            _estadoSimulacion = EstadoSimulacion.Detenida;
            anterior = _corrida;
            _corrida = null;
            anterior?.Cancelacion.Cancel();
        }

        // Una corrida Finalizada ya no tiene hilos útiles; se cierran fuera del lock para que puedan terminar.
        Terminar(anterior);
        Notificar();
    }

    public void Iniciar()
    {
        lock (_estado)
        {
            ComprobarNoLiberado();
            if (_planificador is null)
            {
                throw new InvalidOperationException("No hay procesos cargados: llama a Cargar antes de Iniciar.");
            }

            if (_estadoSimulacion != EstadoSimulacion.Detenida)
            {
                throw new InvalidOperationException($"No se puede iniciar con la simulación {_estadoSimulacion}.");
            }

            CerrarDestino();

            // Antes de despachar, todos los procesos están en Listos: de ahí salen las referencias para sus hilos.
            var procesos = _planificador.Listos.ToArray();
            var corrida = new Corrida(_planificador, _configuracion.ThMs, procesos.Length);
            foreach (var proceso in procesos)
            {
                corrida.Hilos.Add(new Thread(() => CuerpoProceso(corrida, proceso)) { IsBackground = true, Name = $"Proceso-{proceso.Pid}" });
            }

            corrida.Hilos.Add(new Thread(() => CuerpoDispatcher(corrida)) { IsBackground = true, Name = "Dispatcher" });

            _corrida = corrida;
            _estadoSimulacion = EstadoSimulacion.EnCurso;

            // Los hilos arrancan con el estado ya en EnCurso; si piden el lock esperan a que Iniciar lo suelte.
            foreach (var hilo in corrida.Hilos)
            {
                hilo.Start();
            }
        }
    }

    public void Pausar()
    {
        lock (_estado)
        {
            ComprobarNoLiberado();
            switch (_estadoSimulacion)
            {
                case EstadoSimulacion.EnCurso:
                    // Reset bajo el lock: ningún hilo puede copiar un carácter después de que Pausar retorne.
                    CorridaVigente().Pausa.Reset();
                    _estadoSimulacion = EstadoSimulacion.Pausada;
                    break;
                case EstadoSimulacion.Pausada:
                    // Idempotente: pausar dos veces deja la misma pausa.
                    break;
                default:
                    throw new InvalidOperationException($"No se puede pausar una simulación {_estadoSimulacion}.");
            }
        }
    }

    public void Reanudar()
    {
        lock (_estado)
        {
            ComprobarNoLiberado();
            switch (_estadoSimulacion)
            {
                case EstadoSimulacion.Pausada:
                    CorridaVigente().Pausa.Set();
                    _estadoSimulacion = EstadoSimulacion.EnCurso;
                    break;
                case EstadoSimulacion.EnCurso:
                    // Idempotente: reanudar una simulación que ya corre no hace nada.
                    break;
                default:
                    throw new InvalidOperationException($"No se puede reanudar una simulación {_estadoSimulacion}.");
            }
        }
    }

    public void Reiniciar()
    {
        Corrida? descartada;
        lock (_estado)
        {
            ComprobarNoLiberado();
            if (_planificador is null)
            {
                throw new InvalidOperationException("No hay procesos cargados: no hay nada que reiniciar.");
            }

            descartada = DescartarCorrida();
        }

        // El motor ya está Detenida y consistente; esperar a los hilos viejos fuera del lock no deja a nadie viendo un estado a medias.
        Notificar();
        Terminar(descartada);
    }

    public InstantaneaSimulacion ObtenerInstantanea()
    {
        lock (_estado)
        {
            ComprobarNoLiberado();
            return _planificador?.Instantanea(_estadoSimulacion)
                ?? new InstantaneaSimulacion(_estadoSimulacion, 0, [], null, []);
        }
    }

    public IReadOnlyList<FilaReporte> ObtenerReporte()
    {
        lock (_estado)
        {
            ComprobarNoLiberado();
            return _planificador is null ? [] : CalculadoraTiempos.Reporte(_planificador.Terminados);
        }
    }

    public void Dispose()
    {
        Corrida? corrida;
        lock (_estado)
        {
            if (_liberado)
            {
                return;
            }

            _liberado = true;
            corrida = _corrida;
            _corrida = null;
            corrida?.Cancelacion.Cancel();
        }

        Terminar(corrida);

        // Solo se libera el destino que creó el propio motor; el inyectado es de quien lo pasó.
        if (_poseeDestino && _destino is IDisposable disponible)
        {
            disponible.Dispose();
        }
    }

    // Cuerpo del hilo de un proceso: espera su turno, copia y escribe cada carácter, y avisa al dispatcher.
    private void CuerpoProceso(Corrida corrida, ProcesoSimulado proceso)
    {
        try
        {
            bool completo;
            do
            {
                corrida.Turnos[proceso.Numero - 1].Wait(corrida.Token);

                int limite;
                lock (_estado)
                {
                    limite = corrida.Planificador.LimiteDelTurno(proceso);
                }

                for (var copiados = 1; copiados <= limite; copiados++)
                {
                    var caracter = BajoLockSinPausa(corrida, () => corrida.Planificador.CopiarCaracter());
                    EscribirActividad(corrida, proceso, caracter);

                    // Esperar TH tras el último carácter solo retrasaría el cierre (igual que la App 1).
                    if (copiados < limite || !EstaCompleto(proceso))
                    {
                        EsperarTh(corrida);
                    }
                }

                // Se lee antes de avisar: tras el aviso el dispatcher ya puede mover al proceso de estado.
                completo = EstaCompleto(proceso);
                corrida.TurnoTerminado.Release();
            }
            while (!completo);
        }
        catch (OperationCanceledException)
        {
            // Reiniciar o Dispose cancelaron la corrida: el hilo termina sin avisar a nadie.
        }
        catch (Exception)
        {
            // Una excepción sin capturar en un Thread derriba todo el proceso .NET; se contiene cancelando la corrida.
            AbortarPorFallo(corrida);
        }
    }

    // Cuerpo del dispatcher: da la CPU a un proceso, espera a que termine su turno y lo manda a Listo o Terminado.
    private void CuerpoDispatcher(Corrida corrida)
    {
        try
        {
            while (true)
            {
                var proceso = BajoLockSinPausa(corrida, () => SiguienteDespacho(corrida));
                Notificar();
                if (proceso is null)
                {
                    break;
                }

                corrida.Turnos[proceso.Numero - 1].Release();
                corrida.TurnoTerminado.Wait(corrida.Token);

                // Solo se llega aquí con el turno completo; tras una cancelación Wait lanza y FinalizarTurno no se llama.
                BajoLockSinPausa(corrida, () =>
                {
                    corrida.Planificador.FinalizarTurno();
                    return 0;
                });
                Notificar();
            }
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception)
        {
            // Igual que en los procesos: sin esto un fallo del dispatcher dejaría a todos esperando su turno para siempre.
            AbortarPorFallo(corrida);
            return;
        }

        // Si Reiniciar canceló justo ahora, avisar que terminó sería avisar de una corrida que ya no existe.
        if (!corrida.Token.IsCancellationRequested)
        {
            foreach (EventHandler manejador in SimulacionFinalizada?.GetInvocationList() ?? [])
            {
                try
                {
                    manejador(this, EventArgs.Empty);
                }
                catch (Exception)
                {
                    // Un suscriptor que falla (p. ej. una UI ya cerrada) no debe tumbar el hilo ni a los demás suscriptores.
                }
            }
        }
    }

    // Debe llamarse con el lock tomado; devuelve null (y marca Finalizada) cuando ya no queda nadie por ejecutar.
    private ProcesoSimulado? SiguienteDespacho(Corrida corrida)
    {
        if (corrida.Planificador.TodosTerminados)
        {
            _estadoSimulacion = EstadoSimulacion.Finalizada;
            return null;
        }

        // Con la CPU libre todo proceso sin terminar está en Listos; si no, el planificador quedó inconsistente.
        return corrida.Planificador.Despachar()
            ?? throw new InvalidOperationException("Faltan procesos por terminar pero Listos está vacía.");
    }

    // Espera la compuerta de pausa y ejecuta la acción dentro del lock, sin que Pausar pueda colarse entre ambos pasos.
    private T BajoLockSinPausa<T>(Corrida corrida, Func<T> accion)
    {
        while (true)
        {
            corrida.Pausa.Wait(corrida.Token);
            lock (_estado)
            {
                // Un hilo cancelado no debe tocar el planificador: Reiniciar lo descarta y FinalizarTurno lanzaría con un turno a medias.
                corrida.Token.ThrowIfCancellationRequested();

                // Pausar pudo llegar entre la espera y el lock; se vuelve a comprobar dentro para que nada cambie ya pausada.
                if (corrida.Pausa.IsSet)
                {
                    return accion();
                }
            }
        }
    }

    private bool EstaCompleto(ProcesoSimulado proceso)
    {
        lock (_estado)
        {
            return proceso.Completo;
        }
    }

    // El TH real solo da el ritmo visual; la espera se corta al instante si se cancela la corrida.
    private static void EsperarTh(Corrida corrida)
    {
        if (corrida.Token.WaitHandle.WaitOne(corrida.ThMs))
        {
            throw new OperationCanceledException(corrida.Token);
        }
    }

    private PlanificadorRoundRobin CrearPlanificador(IReadOnlyList<ProcesoEntrada> procesos, ConfiguracionSimulacion configuracion)
    {
        try
        {
            // El constructor avisa la llegada de cada proceso: quedan en _pendientes hasta el siguiente Notificar.
            return new PlanificadorRoundRobin(procesos, configuracion, _pendientes.Add);
        }
        catch
        {
            // Los avisos de un planificador que no llegó a existir no deben llegar a nadie.
            _pendientes.Clear();
            throw;
        }
    }

    // Dispara los eventos acumulados fuera del lock; solo la llama el hilo que acaba de provocar los avisos, así salen en orden.
    private void Notificar()
    {
        CambioEstadoEventArgs[] avisos;
        lock (_estado)
        {
            avisos = [.. _pendientes];
            _pendientes.Clear();
        }

        foreach (var aviso in avisos)
        {
            foreach (EventHandler<CambioEstadoEventArgs> manejador in CambioEstado?.GetInvocationList() ?? [])
            {
                try
                {
                    manejador(this, aviso);
                }
                catch (Exception)
                {
                    // El Engine es puro y no registra nada: un suscriptor que falla se ignora para no tumbar el hilo ni a los demás.
                }
            }
        }
    }

    // Espera a los hilos de una corrida ya cancelada con un plazo total; devuelve sin lanzar aunque alguno no termine.
    private static void Terminar(Corrida? corrida)
    {
        if (corrida is null)
        {
            return;
        }

        var cronometro = Stopwatch.StartNew();
        var todosTerminaron = true;
        foreach (var hilo in corrida.Hilos)
        {
            // Un handler puede pedir Reiniciar desde el propio dispatcher; unirse a sí mismo solo esperaría el plazo entero.
            if (hilo == Thread.CurrentThread)
            {
                todosTerminaron = false;
                continue;
            }

            // Join con cero devuelve al instante: una vez agotado el plazo los demás hilos solo se comprueban.
            var restante = _plazoCierre - cronometro.Elapsed;
            if (!hilo.Join(restante > TimeSpan.Zero ? restante : TimeSpan.Zero))
            {
                todosTerminaron = false;
            }
        }

        // Si algún hilo sigue vivo aún podría usar los semáforos; en ese caso los recoge el GC en vez de arriesgar un ObjectDisposedException.
        if (todosTerminaron)
        {
            corrida.Dispose();
        }
    }

    // Debe llamarse con el lock tomado: cancela la corrida y deja el motor Detenida con un planificador nuevo, todo en un solo paso.
    private Corrida? DescartarCorrida()
    {
        var corrida = _corrida;
        _corrida = null;
        corrida?.Cancelacion.Cancel();

        // El planificador de la corrida cancelada se descarta: pudo quedar con un turno a medias.
        _planificador = CrearPlanificador(_procesos, _configuracion);
        _estadoSimulacion = EstadoSimulacion.Detenida;

        // Con el token ya cancelado el destino ignora las escrituras viejas, así que cerrar aquí no pisa nada de una corrida posterior.
        CerrarDestino();
        return corrida;
    }

    // Un fallo inesperado en un hilo cancela la corrida entera: los demás esperarían para siempre a un hilo que ya murió.
    private void AbortarPorFallo(Corrida corrida)
    {
        lock (_estado)
        {
            if (ReferenceEquals(_corrida, corrida))
            {
                DescartarCorrida();
            }
            else
            {
                corrida.Cancelacion.Cancel();
            }
        }

        Notificar();
    }

    // La actividad en archivo es auxiliar: si el destino falla (disco lleno) la simulación continúa.
    private void EscribirActividad(Corrida corrida, ProcesoSimulado proceso, char caracter)
    {
        try
        {
            _destino.Escribir(proceso.Pid, proceso.Nombre, caracter, corrida.Token);
        }
        catch (Exception)
        {
            // Se ignora a propósito: el motor no registra nada y el reporte no depende de lo escrito.
        }
    }

    // Cerrar los archivos también es auxiliar y no puede dejar al motor a medias.
    private void CerrarDestino()
    {
        try
        {
            _destino.NuevaCorrida();
        }
        catch (Exception)
        {
            // Se ignora a propósito: ver EscribirActividad.
        }
    }

    // Pausar y Reanudar solo llegan aquí en EnCurso/Pausada, donde siempre hay corrida; si no, es un error de uso y no un NullReferenceException.
    private Corrida CorridaVigente() =>
        _corrida ?? throw new InvalidOperationException("No hay una corrida activa.");

    private void ComprobarNoLiberado() => ObjectDisposedException.ThrowIf(_liberado, this);

    // Todo lo que pertenece a una sola corrida: un Reiniciar descarta el objeto entero y no deja primitivas viejas en uso.
    private sealed class Corrida : IDisposable
    {
        public Corrida(PlanificadorRoundRobin planificador, int thMs, int procesos)
        {
            Planificador = planificador;
            ThMs = thMs;

            // Un semáforo de turno por proceso, indexado por Numero - 1; (0, 1) hace que un doble permiso lance en vez de colar a dos procesos.
            Turnos = new SemaphoreSlim[procesos];
            for (var i = 0; i < procesos; i++)
            {
                Turnos[i] = new SemaphoreSlim(0, 1);
            }
        }

        public PlanificadorRoundRobin Planificador { get; }

        public int ThMs { get; }

        public CancellationTokenSource Cancelacion { get; } = new();

        public CancellationToken Token => Cancelacion.Token;

        // Abierta (Set) mientras corre; Pausar la cierra con Reset.
        public ManualResetEventSlim Pausa { get; } = new(initialState: true);

        public SemaphoreSlim[] Turnos { get; }

        // El proceso que acaba su turno avisa aquí al dispatcher.
        public SemaphoreSlim TurnoTerminado { get; } = new(0, 1);

        public List<Thread> Hilos { get; } = [];

        public void Dispose()
        {
            Cancelacion.Dispose();
            Pausa.Dispose();
            TurnoTerminado.Dispose();
            foreach (var turno in Turnos)
            {
                turno.Dispose();
            }
        }
    }
}
