using System.IO;
using SimuladorProcesos.Core.Modelos;
using SimuladorProcesos.Desktop.Servicios;

namespace SimuladorProcesos.Desktop.Falsos;

/// <summary>Actividad de mentira: espera TH por carácter y reporta el avance, pero no escribe ningún archivo.</summary>
public sealed class ActividadFalsa : IActividadProceso
{
    public async Task<string> EjecutarAsync(
        ProcesoCatalogo proceso,
        int thMs,
        string carpetaDestino,
        IProgress<int>? progreso = null,
        CancellationToken cancelacion = default)
    {
        for (var copiados = 1; copiados <= proceso.Descripcion.Length; copiados++)
        {
            await Task.Delay(thMs, cancelacion);
            progreso?.Report(copiados);
        }

        return Path.Combine(carpetaDestino, $"{proceso.Nombre}.txt");
    }
}
