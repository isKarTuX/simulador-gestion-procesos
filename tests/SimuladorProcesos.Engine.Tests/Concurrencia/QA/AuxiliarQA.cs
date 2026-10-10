using SimuladorProcesos.Engine.Actividad;

namespace SimuladorProcesos.Engine.Tests.Concurrencia.QA;

/// <summary>
/// Destino que bloquea la primera escritura (antes de guardar el carácter) hasta que la prueba abra la compuerta;
/// simula un disco lento o colgado para alargar Reiniciar/Dispose más allá del plazo del motor.
/// </summary>
internal sealed class DestinoBloqueable : IDestinoActividad
{
    private readonly DestinoEnMemoria _memoria = new();
    private int _porBloquear = 1;

    /// <summary>Se activa cuando un hilo de proceso entró a la escritura bloqueante.</summary>
    public ManualResetEventSlim Entro { get; } = new();

    /// <summary>La prueba la abre (Set) para dejar salir al hilo bloqueado.</summary>
    public ManualResetEventSlim Compuerta { get; } = new();

    public void NuevaCorrida() => _memoria.NuevaCorrida();

    public string Texto(int pid) => _memoria.Texto(pid);

    public void Escribir(int pid, string nombre, char caracter)
    {
        if (Interlocked.Exchange(ref _porBloquear, 0) == 1)
        {
            Entro.Set();
            Compuerta.Wait();
        }

        _memoria.Escribir(pid, nombre, caracter);
    }
}

/// <summary>Destino que falla como un disco lleno o un archivo bloqueado por otro programa.</summary>
internal sealed class DestinoQueLanza : IDestinoActividad
{
    public void NuevaCorrida()
    {
    }

    public void Escribir(int pid, string nombre, char caracter) => throw new IOException("Disco lleno (simulado por QA).");
}
