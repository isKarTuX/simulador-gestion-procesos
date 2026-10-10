using System.IO;
using System.Text;
using SimuladorProcesos.Core.Modelos;

namespace SimuladorProcesos.Desktop.Servicios;

/// <summary>M3: copia la descripción del proceso a un .txt carácter por carácter, esperando TH ms entre caracteres.</summary>
public sealed class ActividadProceso : IActividadProceso
{
    // Sin BOM para que el .txt contenga exactamente la descripción.
    private static readonly UTF8Encoding _utf8SinBom = new(encoderShouldEmitUTF8Identifier: false);

    public async Task<string> EjecutarAsync(
        ProcesoCatalogo proceso,
        int thMs,
        string carpetaDestino,
        IProgress<int>? progreso = null,
        CancellationToken cancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(proceso);
        ArgumentException.ThrowIfNullOrWhiteSpace(carpetaDestino);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(thMs, 0);
        cancelacion.ThrowIfCancellationRequested();

        Directory.CreateDirectory(carpetaDestino);
        var ruta = Path.Combine(carpetaDestino, NombreArchivoActividad.Para(proceso.Nombre, proceso.Pid));
        var descripcion = proceso.Descripcion ?? string.Empty;

        // FileShare.Read: el archivo se puede abrir para leer mientras todavía se escribe.
        await using var flujo = new FileStream(
            ruta, FileMode.Create, FileAccess.Write, FileShare.Read, bufferSize: 4096, useAsync: true);

        // El codificador conserva el estado entre llamadas, así un par sustituto (emoji) partido en dos char sale como UTF-8 válido.
        var codificador = _utf8SinBom.GetEncoder();
        var bytes = new byte[_utf8SinBom.GetMaxByteCount(1)];

        for (var i = 0; i < descripcion.Length; i++)
        {
            var escritos = codificador.GetBytes(descripcion.AsSpan(i, 1), bytes, flush: false);
            if (escritos > 0)
            {
                await flujo.WriteAsync(bytes.AsMemory(0, escritos), cancelacion);
            }

            // Flush por carácter para que quien abra el archivo vea el avance, no solo al final.
            await flujo.FlushAsync(cancelacion);
            progreso?.Report(i + 1);

            // Entre caracteres, no tras el último: esperar al terminar solo retrasaría el cierre.
            if (i < descripcion.Length - 1)
            {
                await Task.Delay(thMs, cancelacion);
            }
        }

        return ruta;
    }
}
