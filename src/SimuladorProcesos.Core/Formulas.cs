namespace SimuladorProcesos.Core;

/// <summary>Fórmulas del enunciado (CONTEXT §2.4). Todos los tiempos en milisegundos.</summary>
public static class Formulas
{
    /// <summary>R = TH × cantidad de caracteres de la descripción.</summary>
    public static long Rafaga(int thMs, int caracteres)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(thMs);
        ArgumentOutOfRangeException.ThrowIfNegative(caracteres);
        return (long)thMs * caracteres;
    }

    /// <summary>TL = 0 si Listo está vacío; si no, TL del último proceso + 1.</summary>
    public static long TiempoLlegada(bool listoVacio, long tiempoLlegadaUltimo) =>
        listoVacio ? 0 : tiempoLlegadaUltimo + 1;

    /// <summary>TF del enunciado = Quantum × #Ejecuciones (veces que el proceso usó la CPU).</summary>
    public static long TiempoFinalizacionEnunciado(int quantum, int ejecuciones) => (long)quantum * ejecuciones;

    /// <summary>TAT = TF − TL.</summary>
    public static long Turnaround(long tiempoFinalizacion, long tiempoLlegada) => tiempoFinalizacion - tiempoLlegada;

    /// <summary>Espera = TAT − R.</summary>
    public static long Espera(long turnaround, long rafaga) => turnaround - rafaga;
}
