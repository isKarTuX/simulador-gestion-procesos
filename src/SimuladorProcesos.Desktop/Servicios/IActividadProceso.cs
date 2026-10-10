using SimuladorProcesos.Core.Modelos;

namespace SimuladorProcesos.Desktop.Servicios;

/// <summary>M3: actividad de un proceso. Implementa: subtarea 1.5 en Servicios/Actividad.</summary>
public interface IActividadProceso
{
    /// <summary>
    /// Crea "&lt;nombre saneado&gt;.txt" en <paramref name="carpetaDestino"/> y copia la descripción carácter por carácter,
    /// esperando <paramref name="thMs"/> entre caracteres. Reporta los caracteres copiados y devuelve la ruta del archivo.
    /// </summary>
    Task<string> EjecutarAsync(
        ProcesoCatalogo proceso,
        int thMs,
        string carpetaDestino,
        IProgress<int>? progreso = null,
        CancellationToken cancelacion = default);
}
