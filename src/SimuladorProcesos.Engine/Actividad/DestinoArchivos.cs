using System.Text;

namespace SimuladorProcesos.Engine.Actividad;

/// <summary>Escribe la actividad de cada proceso en "&lt;carpeta&gt;/&lt;nombre&gt;_&lt;pid&gt;.txt", un carácter por vez.</summary>
public sealed class DestinoArchivos : IDestinoActividad, IDisposable
{
    // Sin BOM para que el .txt contenga exactamente la descripción.
    private static readonly UTF8Encoding _utf8SinBom = new(encoderShouldEmitUTF8Identifier: false);

    private readonly string _carpeta;
    private readonly object _candado = new();
    private readonly Dictionary<string, StreamWriter> _escritores = [];

    public DestinoArchivos(string carpeta)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(carpeta);
        _carpeta = carpeta;
    }

    public void NuevaCorrida()
    {
        // Se cierran los archivos para que el primer carácter de la corrida nueva los recree vacíos.
        lock (_candado)
        {
            CerrarEscritores();
        }
    }

    public void Escribir(int pid, string nombre, char caracter, CancellationToken cancelacion)
    {
        var archivo = NombreArchivoActividad.Para(nombre, pid);
        lock (_candado)
        {
            // Se comprueba dentro del lock: así una escritura vieja no puede reabrir el archivo después de NuevaCorrida.
            if (cancelacion.IsCancellationRequested)
            {
                return;
            }

            if (!_escritores.TryGetValue(archivo, out var escritor))
            {
                Directory.CreateDirectory(_carpeta);

                // FileShare.ReadWrite|Delete: el archivo se puede leer (o borrar) mientras todavía se escribe.
                var flujo = new FileStream(Path.Combine(_carpeta, archivo), FileMode.Create, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);

                // AutoFlush por carácter para que quien abra el archivo vea el avance; el escritor guarda un par sustituto partido en dos char.
                escritor = new StreamWriter(flujo, _utf8SinBom) { AutoFlush = true };
                _escritores[archivo] = escritor;
            }

            escritor.Write(caracter);
        }
    }

    public void Dispose()
    {
        lock (_candado)
        {
            CerrarEscritores();
        }
    }

    private void CerrarEscritores()
    {
        foreach (var escritor in _escritores.Values)
        {
            try
            {
                escritor.Dispose();
            }
            catch (IOException)
            {
                // Cerrar es de mejor esfuerzo: un disco que falla al vaciar el búfer no debe impedir cerrar los demás archivos.
            }
        }

        _escritores.Clear();
    }
}
