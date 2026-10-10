using SimuladorProcesos.Core;
using SimuladorProcesos.Engine.Estructuras;
using SimuladorProcesos.Engine.Modelo;

namespace SimuladorProcesos.Engine.Planificacion;

/// <summary>
/// Lógica Round Robin sin hilos ni esperas: cada llamada es un paso determinista y el reloj es solo un contador.
/// SimuladorRoundRobin la envuelve con hilos; no es thread-safe, el motor la protege con su lock (ADR-0002).
/// Un turno completo es: Despachar, LimiteDelTurno veces CopiarCaracter, FinalizarTurno.
/// </summary>
internal sealed class PlanificadorRoundRobin
{
    private readonly ConfiguracionSimulacion _configuracion;
    private readonly Action<CambioEstadoEventArgs> _alCambiar;
    private readonly int _totalProcesos;

    // Se fijan al despachar para que un turno no pueda alargarse ni cortarse a mitad.
    private int _limiteTurno;
    private int _copiadosEnTurno;

    public PlanificadorRoundRobin(
        IReadOnlyList<ProcesoEntrada> procesos,
        ConfiguracionSimulacion configuracion,
        Action<CambioEstadoEventArgs> alCambiar)
    {
        ArgumentNullException.ThrowIfNull(procesos);
        ArgumentNullException.ThrowIfNull(configuracion);
        ArgumentNullException.ThrowIfNull(alCambiar);

        _configuracion = configuracion;
        _alCambiar = alCambiar;
        _totalProcesos = procesos.Count;

        long tiempoLlegadaUltimo = 0;
        for (var i = 0; i < procesos.Count; i++)
        {
            var entrada = procesos[i];
            ArgumentNullException.ThrowIfNull(entrada);

            // Un quantum de 0 daría turnos sin progreso y la simulación nunca terminaría.
            if (entrada.QuantumPropio is <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(procesos), $"El quantum propio de '{entrada.Nombre}' debe ser mayor que cero.");
            }

            var proceso = new ProcesoSimulado(i + 1, entrada, configuracion);
            proceso.TiempoLlegada = Formulas.TiempoLlegada(Listos.EstaVacia, tiempoLlegadaUltimo);
            tiempoLlegadaUltimo = proceso.TiempoLlegada;
            Listos.Encolar(proceso);
            Avisar(proceso, null, EstadoProceso.Listo);
        }
    }

    /// <summary>Reloj simulado en ms: avanza TH por cada carácter copiado.</summary>
    public long RelojMs { get; private set; }

    /// <summary>Estado Listo: cola FIFO propia.</summary>
    public ColaEnlazada<ProcesoSimulado> Listos { get; } = new();

    /// <summary>Estado Ejecución: un solo slot, null si la CPU está libre.</summary>
    public ProcesoSimulado? EnEjecucion { get; private set; }

    /// <summary>Estado Terminado: lista propia en orden de finalización.</summary>
    public ListaEnlazada<ProcesoSimulado> Terminados { get; } = new();

    public bool TodosTerminados => Terminados.Cantidad == _totalProcesos;

    /// <summary>Listo → Ejecución. Devuelve el proceso que toma la CPU, o null si no hay nadie esperando.</summary>
    public ProcesoSimulado? Despachar()
    {
        if (EnEjecucion is not null)
        {
            throw new InvalidOperationException($"La CPU está ocupada por '{EnEjecucion.Nombre}': hay que finalizar su turno antes de despachar.");
        }

        if (Listos.EstaVacia)
        {
            return null;
        }

        var proceso = Listos.Desencolar();
        proceso.Estado = EstadoProceso.Ejecucion;
        proceso.Ejecuciones++;
        EnEjecucion = proceso;
        _limiteTurno = LimiteDelTurno(proceso);
        _copiadosEnTurno = 0;
        Avisar(proceso, EstadoProceso.Listo, EstadoProceso.Ejecucion);
        return proceso;
    }

    /// <summary>Caracteres que copia en este turno: todos los que le faltan si no es expulsivo, a lo sumo su quantum si lo es.</summary>
    public int LimiteDelTurno(ProcesoSimulado proceso)
    {
        ArgumentNullException.ThrowIfNull(proceso);
        return proceso.EsExpulsivo ? Math.Min(proceso.Quantum, proceso.CaracteresRestantes) : proceso.CaracteresRestantes;
    }

    /// <summary>Copia el siguiente carácter del proceso en ejecución y avanza el reloj TH ms; el llamador lo escribe al archivo.</summary>
    public char CopiarCaracter()
    {
        var proceso = EnEjecucion ?? throw new InvalidOperationException("No hay proceso en ejecución: no se puede copiar un carácter.");
        if (_copiadosEnTurno >= _limiteTurno)
        {
            throw new InvalidOperationException($"'{proceso.Nombre}' ya copió los {_limiteTurno} caracteres de su turno.");
        }

        var caracter = proceso.SiguienteCaracter;
        proceso.CaracteresCopiados++;
        _copiadosEnTurno++;
        RelojMs += _configuracion.ThMs;
        return caracter;
    }

    /// <summary>Ejecución → Terminado si la actividad quedó completa; si no (solo un expulsivo), Ejecución → Listo al final de la cola.</summary>
    public void FinalizarTurno()
    {
        var proceso = EnEjecucion ?? throw new InvalidOperationException("No hay proceso en ejecución: no hay turno que finalizar.");
        if (_copiadosEnTurno < _limiteTurno)
        {
            throw new InvalidOperationException($"'{proceso.Nombre}' copió {_copiadosEnTurno} de los {_limiteTurno} caracteres de su turno.");
        }

        // Todo el estado cambia antes de avisar, para que quien escuche vea un motor consistente.
        EnEjecucion = null;
        if (proceso.Completo)
        {
            proceso.Estado = EstadoProceso.Terminado;
            proceso.TiempoFinalizacionReal = RelojMs;
            Terminados.Agregar(proceso);
            Avisar(proceso, EstadoProceso.Ejecucion, EstadoProceso.Terminado);
        }
        else
        {
            // Un no expulsivo no llega aquí: su límite es todo lo que le falta, así que siempre queda completo.
            proceso.Estado = EstadoProceso.Listo;
            Listos.Encolar(proceso);
            Avisar(proceso, EstadoProceso.Ejecucion, EstadoProceso.Listo);
        }
    }

    /// <summary>Foto inmutable para la UI; las listas de solo lectura se copian para que ningún cambio posterior las altere.</summary>
    public InstantaneaSimulacion Instantanea(EstadoSimulacion estado) => new(
        estado,
        RelojMs,
        Listos.Select(p => p.Instantanea()).ToArray(),
        EnEjecucion?.Instantanea(),
        Terminados.Select(p => p.Instantanea()).ToArray());

    private void Avisar(ProcesoSimulado proceso, EstadoProceso? desde, EstadoProceso hacia) =>
        _alCambiar(new CambioEstadoEventArgs(proceso.Instantanea(), desde, hacia, RelojMs));
}
