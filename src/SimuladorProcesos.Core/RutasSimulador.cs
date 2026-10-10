namespace SimuladorProcesos.Core;

/// <summary>Ubicaciones compartidas por la App 1 y la Api para encontrar los mismos datos.</summary>
public static class RutasSimulador
{
    public static string CarpetaDatos { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "SimuladorProcesos");

    public static string BaseDatosPorDefecto { get; } = Path.Combine(CarpetaDatos, "simulador.db");

    public static string CadenaConexionPorDefecto { get; } = $"Data Source={BaseDatosPorDefecto}";

    /// <summary>Carpeta donde cada proceso escribe su archivo de actividad.</summary>
    public static string CarpetaActividad { get; } = Path.Combine(CarpetaDatos, "actividad");
}
