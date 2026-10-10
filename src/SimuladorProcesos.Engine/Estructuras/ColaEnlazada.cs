using System.Collections;

namespace SimuladorProcesos.Engine.Estructuras;

/// <summary>
/// Cola FIFO con nodos enlazados propios para el estado Listo (ADR-0003).
/// Firma congelada; el cuerpo lo implementa la subtarea 1.7 (docs/PROMPTS.md).
/// </summary>
internal sealed class ColaEnlazada<T> : IEnumerable<T>
{
    public int Cantidad => throw new NotImplementedException("Subtarea 1.7");

    public bool EstaVacia => Cantidad == 0;

    /// <summary>Agrega al final en O(1).</summary>
    public void Encolar(T valor) => throw new NotImplementedException("Subtarea 1.7");

    /// <summary>Quita y devuelve el primero en O(1). Lanza InvalidOperationException si está vacía.</summary>
    public T Desencolar() => throw new NotImplementedException("Subtarea 1.7");

    /// <summary>Devuelve el primero sin quitarlo. Lanza InvalidOperationException si está vacía.</summary>
    public T VerFrente() => throw new NotImplementedException("Subtarea 1.7");

    public void Vaciar() => throw new NotImplementedException("Subtarea 1.7");

    /// <summary>Recorre del frente al final.</summary>
    public IEnumerator<T> GetEnumerator() => throw new NotImplementedException("Subtarea 1.7");

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
