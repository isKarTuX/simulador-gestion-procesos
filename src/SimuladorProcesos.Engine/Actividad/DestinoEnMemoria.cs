using System.Text;

namespace SimuladorProcesos.Engine.Actividad;

/// <summary>Destino para pruebas: acumula el texto de cada proceso en memoria, sin tocar el disco.</summary>
public sealed class DestinoEnMemoria : IDestinoActividad
{
    private readonly object _candado = new();
    private readonly Dictionary<int, StringBuilder> _textos = [];
    private int _corridas;

    /// <summary>Veces que el motor pidió empezar de cero (cada Iniciar y cada Reiniciar); las pruebas lo usan para ver que limpia.</summary>
    public int Corridas
    {
        get
        {
            lock (_candado)
            {
                return _corridas;
            }
        }
    }

    public void NuevaCorrida()
    {
        lock (_candado)
        {
            _textos.Clear();
            _corridas++;
        }
    }

    public void Escribir(int pid, string nombre, char caracter, CancellationToken cancelacion)
    {
        // El lock es necesario porque la prueba lee el texto desde su hilo mientras el proceso aún escribe.
        lock (_candado)
        {
            // Se comprueba dentro del lock: así una escritura vieja no puede colarse después de NuevaCorrida.
            if (cancelacion.IsCancellationRequested)
            {
                return;
            }

            if (!_textos.TryGetValue(pid, out var texto))
            {
                texto = new StringBuilder();
                _textos[pid] = texto;
            }

            texto.Append(caracter);
        }
    }

    /// <summary>Texto escrito hasta ahora por el proceso; vacío si nunca escribió (por ejemplo, descripción vacía).</summary>
    public string Texto(int pid)
    {
        lock (_candado)
        {
            return _textos.TryGetValue(pid, out var texto) ? texto.ToString() : string.Empty;
        }
    }
}
