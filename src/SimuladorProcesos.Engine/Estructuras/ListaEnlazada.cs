using System.Collections;

namespace SimuladorProcesos.Engine.Estructuras;

/// <summary>
/// Lista simplemente enlazada con inserción al final para el estado Terminado (ADR-0003).
/// No es thread-safe: el motor la protege con su propio lock.
/// </summary>
internal sealed class ListaEnlazada<T> : IEnumerable<T>
{
    private Nodo<T>? _primero;
    private Nodo<T>? _ultimo;
    private int _cantidad;
    private int _version;

    public int Cantidad => _cantidad;

    public bool EstaVacia => Cantidad == 0;

    /// <summary>Agrega al final en O(1) (mantiene puntero al último nodo).</summary>
    public void Agregar(T valor)
    {
        var nodo = new Nodo<T>(valor);
        if (_ultimo is null)
        {
            _primero = nodo;
        }
        else
        {
            _ultimo.Siguiente = nodo;
        }

        _ultimo = nodo;
        _cantidad++;
        _version++;
    }

    public void Vaciar()
    {
        // Se sueltan los enlaces nodo a nodo para que ninguna referencia vieja mantenga vivos los valores.
        var actual = _primero;
        while (actual is not null)
        {
            var siguiente = actual.Siguiente;
            actual.Siguiente = null;
            actual = siguiente;
        }

        _primero = null;
        _ultimo = null;
        _cantidad = 0;
        _version++;
    }

    /// <summary>Recorre en orden de inserción.</summary>
    public IEnumerator<T> GetEnumerator()
    {
        // Si la lista cambia a mitad del recorrido los punteros ya no son confiables: se falla en vez de dar datos inconsistentes.
        var versionInicial = _version;
        for (var actual = _primero; actual is not null; actual = actual.Siguiente)
        {
            yield return actual.Valor;
            if (versionInicial != _version)
            {
                throw new InvalidOperationException("La lista cambió durante el recorrido.");
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
