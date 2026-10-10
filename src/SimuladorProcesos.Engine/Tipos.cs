using SimuladorProcesos.Core.Modelos;

namespace SimuladorProcesos.Engine;

public enum EstadoProceso
{
    Listo,
    Ejecucion,
    Terminado,
}

public enum EstadoSimulacion
{
    Detenida,
    EnCurso,
    Pausada,
    Finalizada,
}

/// <summary>Parámetros que el usuario define en la App 2.</summary>
public sealed record ConfiguracionSimulacion
{
    public ConfiguracionSimulacion(int thMs, int quantum)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(thMs);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantum);
        ThMs = thMs;
        Quantum = quantum;
    }

    /// <summary>Milisegundos por carácter copiado.</summary>
    public int ThMs { get; }

    /// <summary>Caracteres que un proceso expulsivo copia por turno (CONTEXT §3, duda 2).</summary>
    public int Quantum { get; }
}

/// <summary>Proceso tal como llega del servicio REST.</summary>
/// <param name="QuantumPropio">Override opcional del quantum global para este proceso.</param>
public sealed record ProcesoEntrada(
    int Pid,
    string Nombre,
    string Descripcion,
    Prioridad Prioridad,
    int? QuantumPropio = null);

/// <summary>Foto inmutable de un proceso para la UI.</summary>
/// <param name="Numero">Orden de carga (1..N), se muestra como "Procesos N".</param>
/// <param name="Ejecuciones">Veces que el proceso usó la CPU.</param>
/// <param name="EnEspera">True si está en Listo y ya usó la CPU al menos una vez (banda Espera).</param>
public sealed record ProcesoInstantanea(
    int Numero,
    int Pid,
    string Nombre,
    Prioridad Prioridad,
    EstadoProceso Estado,
    int Quantum,
    int Ejecuciones,
    int CaracteresTotales,
    int CaracteresRestantes,
    bool EnEspera,
    long TiempoLlegada,
    long Rafaga,
    long? TiempoFinalizacion);

/// <summary>Foto inmutable de toda la simulación.</summary>
public sealed record InstantaneaSimulacion(
    EstadoSimulacion Estado,
    long RelojMs,
    IReadOnlyList<ProcesoInstantanea> Listos,
    ProcesoInstantanea? Ejecucion,
    IReadOnlyList<ProcesoInstantanea> Terminados)
{
    /// <summary>Banda "Espera" de la GUI: vista derivada de Listos.</summary>
    public IEnumerable<ProcesoInstantanea> Espera => Listos.Where(p => p.EnEspera);
}

public sealed class CambioEstadoEventArgs(ProcesoInstantanea proceso, EstadoProceso? desde, EstadoProceso hacia, long relojMs)
    : EventArgs
{
    public ProcesoInstantanea Proceso { get; } = proceso;

    /// <summary>Null cuando el proceso recién llega a Listo.</summary>
    public EstadoProceso? Desde { get; } = desde;

    public EstadoProceso Hacia { get; } = hacia;

    public long RelojMs { get; } = relojMs;
}

/// <summary>Fila de los reportes: P, T.L, R, P.R, T.R, T.F (más TF real y espera para Anexos).</summary>
public sealed record FilaReporte(
    string Proceso,
    long TiempoLlegada,
    long Rafaga,
    int PrioridadAsignada,
    long Turnaround,
    long TiempoFinalizacion,
    long TiempoFinalizacionReal,
    long Espera);
