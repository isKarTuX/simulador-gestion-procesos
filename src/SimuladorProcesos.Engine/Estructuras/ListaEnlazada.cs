using System.Collections;

namespace SimuladorProcesos.Engine.Estructuras;

/// <summary>
/// Lista simplemente enlazada con inserción al final para el estado Terminado (ADR-0003).
/// Firma congelada; el cuerpo lo implementa la subtarea 1.7 (docs/PROMPTS.md).
/// </summary>
internal sealed class ListaEnlazada<T> : IEnumerable<T>
{
    public int Cantidad => throw new NotImplementedException("Subtarea 1.7");

    public bool EstaVacia => Cantidad == 0;

    /// <summary>Agrega al final en O(1) (mantiene puntero al último nodo).</summary>
    public void Agregar(T valor) => throw new NotImplementedException("Subtarea 1.7");

    public void Vaciar() => throw new NotImplementedException("Subtarea 1.7");

    /// <summary>Recorre en orden de inserción.</summary>
    public IEnumerator<T> GetEnumerator() => throw new NotImplementedException("Subtarea 1.7");

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
