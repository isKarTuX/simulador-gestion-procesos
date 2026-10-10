namespace SimuladorProcesos.Engine.Estructuras;

/// <summary>Nodo de una estructura enlazada: guarda un valor y apunta al siguiente (o a null si es el último).</summary>
internal sealed class Nodo<T>(T valor)
{
    public T Valor { get; } = valor;

    public Nodo<T>? Siguiente { get; set; }
}
