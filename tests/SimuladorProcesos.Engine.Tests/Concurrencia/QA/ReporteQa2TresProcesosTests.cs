using SimuladorProcesos.Core.Modelos;
using SimuladorProcesos.Engine.Actividad;
using static SimuladorProcesos.Engine.Tests.Concurrencia.AuxiliarSimulador;

namespace SimuladorProcesos.Engine.Tests.Concurrencia.QA;

/// <summary>
/// QA (segunda vuelta): catálogo propio de 3 procesos calculado a mano contra ObtenerReporte().
/// Difiere del de 2.1 en TH, quantum y composición: incluye una descripción vacía y un quantum que no divide exacto.
/// </summary>
[Trait("Category", "Concurrencia")]
public class ReporteQa2TresProcesosTests
{
    // Cálculo a mano. Alfa = expulsivo "abcd" (4), Beto = NO expulsivo "xy" (2), Cora = expulsivo "" (0). TH = 5, quantum = 3.
    //   TL: Alfa = 0 (Listos vacía), Beto = 0 + 1 = 1, Cora = 1 + 1 = 2.   R = TH x caracteres: Alfa = 20, Beto = 10, Cora = 0.
    //   turno | proceso | copia | reloj | consecuencia
    //   1     | Alfa    | abc   | 15    | agotó el quantum, vuelve a Listos        (Listos: Beto, Cora, Alfa)
    //   2     | Beto    | xy    | 25    | no expulsivo: corre hasta terminar       (Terminado, TF real 25)
    //   3     | Cora    | (nada)| 25    | descripción vacía: termina sin copiar    (Terminado, TF real 25)
    //   4     | Alfa    | d     | 30    | Terminado, TF real 30
    // Ejecuciones: Alfa = 2, Beto = 1, Cora = 1.   TF enunciado = quantum x ejecuciones: Alfa = 6, Beto = 3, Cora = 3.
    // T.R (TAT) = TF real - TL:  Beto = 25 - 1 = 24,  Cora = 25 - 2 = 23,  Alfa = 30 - 0 = 30.
    // Espera = T.R - R:          Beto = 24 - 10 = 14, Cora = 23 - 0 = 23,  Alfa = 30 - 20 = 10.
    // Orden de terminación (el del reporte): Beto, Cora, Alfa. Reloj final = 6 caracteres x 5 ms = 30.
    private static readonly ProcesoEntrada[] _procesos =
    [
        new(201, "Alfa", "abcd", Prioridad.Expulsivo),
        new(202, "Beto", "xy", Prioridad.NoExpulsivo),
        new(203, "Cora", string.Empty, Prioridad.Expulsivo),
    ];

    [Fact]
    public void El_reporte_de_3_procesos_con_TH_5_y_quantum_3_coincide_con_el_calculo_a_mano()
    {
        var destino = new DestinoEnMemoria();
        using var simulador = Cargado(destino, _procesos, 3, thMs: 5);

        EjecutarHastaElFinal(simulador);

        FilaReporte[] esperado =
        [
            new("Beto", TiempoLlegada: 1, Rafaga: 10, PrioridadAsignada: 1, Turnaround: 24, TiempoFinalizacion: 3, TiempoFinalizacionReal: 25, Espera: 14),
            new("Cora", TiempoLlegada: 2, Rafaga: 0, PrioridadAsignada: 0, Turnaround: 23, TiempoFinalizacion: 3, TiempoFinalizacionReal: 25, Espera: 23),
            new("Alfa", TiempoLlegada: 0, Rafaga: 20, PrioridadAsignada: 0, Turnaround: 30, TiempoFinalizacion: 6, TiempoFinalizacionReal: 30, Espera: 10),
        ];
        Assert.Equal(esperado, simulador.ObtenerReporte());
        Assert.Equal(30, simulador.ObtenerInstantanea().RelojMs);
        Assert.Equal("abcd", destino.Texto(201));
        Assert.Equal("xy", destino.Texto(202));
        Assert.Equal(string.Empty, destino.Texto(203));
    }
}
