using SimuladorProcesos.Core;
using SimuladorProcesos.Core.Modelos;

namespace SimuladorProcesos.Engine.Modelo;

/// <summary>
/// Estado mutable de un proceso dentro del motor (su PCB simplificado).
/// Compartido por Planificacion, Calculos y SimuladorRoundRobin; solo se modifica dentro del lock del motor.
/// </summary>
internal sealed class ProcesoSimulado
{
    public ProcesoSimulado(int numero, ProcesoEntrada entrada, ConfiguracionSimulacion configuracion)
    {
        Numero = numero;
        Entrada = entrada;
        Quantum = entrada.QuantumPropio ?? configuracion.Quantum;
        Rafaga = Formulas.Rafaga(configuracion.ThMs, entrada.Descripcion.Length);
    }

    /// <summary>Orden de carga 1..N ("Procesos N" en la tarjeta).</summary>
    public int Numero { get; }

    public ProcesoEntrada Entrada { get; }

    public int Pid => Entrada.Pid;

    public string Nombre => Entrada.Nombre;

    public Prioridad Prioridad => Entrada.Prioridad;

    public bool EsExpulsivo => Prioridad == Prioridad.Expulsivo;

    /// <summary>Caracteres que copia por turno si es expulsivo.</summary>
    public int Quantum { get; }

    public int CaracteresTotales => Entrada.Descripcion.Length;

    public int CaracteresCopiados { get; set; }

    public int CaracteresRestantes => CaracteresTotales - CaracteresCopiados;

    public bool Completo => CaracteresRestantes == 0;

    /// <summary>Veces que el proceso obtuvo la CPU.</summary>
    public int Ejecuciones { get; set; }

    public EstadoProceso Estado { get; set; } = EstadoProceso.Listo;

    public long TiempoLlegada { get; set; }

    /// <summary>R = TH × caracteres.</summary>
    public long Rafaga { get; }

    /// <summary>Reloj simulado (ms) en el que pasó a Terminado.</summary>
    public long? TiempoFinalizacionReal { get; set; }

    /// <summary>Banda "Espera": en Listo y ya usó la CPU al menos una vez.</summary>
    public bool EnEspera => Estado == EstadoProceso.Listo && Ejecuciones > 0;

    public char SiguienteCaracter => Entrada.Descripcion[CaracteresCopiados];

    public ProcesoInstantanea Instantanea() => new(
        Numero,
        Pid,
        Nombre,
        Prioridad,
        Estado,
        Quantum,
        Ejecuciones,
        CaracteresTotales,
        CaracteresRestantes,
        EnEspera,
        TiempoLlegada,
        Rafaga,
        TiempoFinalizacionReal);
}
