using System.Collections;

namespace SimuladorProcesos.Engine.Estructuras;

/// <summary>
/// Cola FIFO con nodos enlazados propios para el estado Listo (ADR-0003).
/// No es thread-safe: el motor la protege con su propio lock.
/// </summary>
internal sealed class ColaEnlazada<T> : IEnumerable<T>
{
    private Nodo<T>? _cabeza;
    private Nodo<T>? _cola;
    private int _cantidad;
    private int _version;

    public int Cantidad => _cantidad;

    public bool EstaVacia => Cantidad == 0;

    /// <summary>Agrega al final en O(1).</summary>
    public void Encolar(T valor)
    {
        var nodo = new Nodo<T>(valor);
        if (_cola is null)
        {
            _cabeza = nodo;
        }
        else
        {
            _cola.Siguiente = nodo;
        }

        _cola = nodo;
        _cantidad++;
        _version++;
    }

    /// <summary>Quita y devuelve el primero en O(1). Lanza InvalidOperationException si está vacía.</summary>
    public T Desencolar()
    {
        var primero = _cabeza ?? throw new InvalidOperationException("No se puede desencolar: la cola está vacía.");
        _cabeza = primero.Siguiente;
        if (_cabeza is null)
        {
            _cola = null;
        }

        primero.Siguiente = null;
        _cantidad--;
        _version++;
        return primero.Valor;
    }

    /// <summary>Devuelve el primero sin quitarlo. Lanza InvalidOperationException si está vacía.</summary>
    public T VerFrente() =>
        (_cabeza ?? throw new InvalidOperationException("No hay frente que ver: la cola está vacía.")).Valor;

    public void Vaciar()
    {
        // Se sueltan los enlaces nodo a nodo para que ninguna referencia vieja mantenga vivos los valores.
        var actual = _cabeza;
        while (actual is not null)
        {
            var siguiente = actual.Siguiente;
            actual.Siguiente = null;
            actual = siguiente;
        }

        _cabeza = null;
        _cola = null;
        _cantidad = 0;
        _version++;
    }

    /// <summary>Recorre del frente al final.</summary>
    public IEnumerator<T> GetEnumerator()
    {
        // Si la cola cambia a mitad del recorrido los punteros ya no son confiables: se falla en vez de dar datos inconsistentes.
        var versionInicial = _version;
        for (var actual = _cabeza; actual is not null; actual = actual.Siguiente)
        {
            yield return actual.Valor;
            if (versionInicial != _version)
            {
                throw new InvalidOperationException("La cola cambió durante el recorrido.");
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
