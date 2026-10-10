namespace SimuladorProcesos.Desktop.Servicios.Recolector;

/// <summary>Lectura cruda de un proceso; un dato en null significa que no se pudo leer (por ejemplo, Access Denied).</summary>
public sealed record MuestraProceso(
    int Pid,
    string Nombre,
    long? MemoriaBytes = null,
    TimeSpan? CpuInicial = null,
    TimeSpan? CpuFinal = null);
