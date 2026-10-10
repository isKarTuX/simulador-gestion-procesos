using SimuladorProcesos.Core.Modelos;
using SimuladorProcesos.Engine.Actividad;
using static SimuladorProcesos.Engine.Tests.Concurrencia.AuxiliarSimulador;

namespace SimuladorProcesos.Engine.Tests.Concurrencia.QA;

/// <summary>QA: catálogo de 3 procesos calculado a mano (TH = 10 ms, quantum = 2) contra ObtenerReporte().</summary>
[Trait("Category", "Concurrencia")]
public class ReporteTresProcesosTests
{
    // Cálculo a mano. P1 = expulsivo "abcde" (5), P2 = NO expulsivo "wxyz" (4), P3 = expulsivo "123" (3). TH = 10, quantum = 2.
    //   TL: P1 = 0 (Listos vacía), P2 = 0 + 1 = 1, P3 = 1 + 1 = 2.   R = TH x caracteres: P1 = 50, P2 = 40, P3 = 30.
    //   turno | proceso | copia  | reloj | consecuencia
    //   1     | P1      | ab     | 20    | agotó el quantum, vuelve a Listos        (Listos: P2, P3, P1)
    //   2     | P2      | wxyz   | 60    | no expulsivo: corre hasta terminar       (Terminado, TF real 60)
    //   3     | P3      | 12     | 80    | vuelve a Listos                          (Listos: P1, P3)
    //   4     | P1      | cd     | 100   | vuelve a Listos                          (Listos: P3, P1)
    //   5     | P3      | 3      | 110   | Terminado, TF real 110
    //   6     | P1      | e      | 120   | Terminado, TF real 120
    // Ejecuciones: P1 = 3, P2 = 1, P3 = 2.   TF enunciado = quantum x ejecuciones: P1 = 6, P2 = 2, P3 = 4.
    // T.R (TAT) = TF real - TL:  P2 = 60 - 1 = 59,  P3 = 110 - 2 = 108,  P1 = 120 - 0 = 120.
    // Espera = T.R - R:          P2 = 59 - 40 = 19, P3 = 108 - 30 = 78,  P1 = 120 - 50 = 70.
    // Orden de terminación (el del reporte): P2, P3, P1.
    private static readonly ProcesoEntrada[] _procesos =
    [
        new(101, "P1", "abcde", Prioridad.Expulsivo),
        new(102, "P2", "wxyz", Prioridad.NoExpulsivo),
        new(103, "P3", "123", Prioridad.Expulsivo),
    ];

    [Fact]
    public void El_reporte_de_3_procesos_con_TH_10_y_quantum_2_coincide_con_el_calculo_a_mano()
    {
        var destino = new DestinoEnMemoria();
        using var simulador = Cargado(destino, _procesos, 2, thMs: 10);

        EjecutarHastaElFinal(simulador);

        FilaReporte[] esperado =
        [
            new("P2", TiempoLlegada: 1, Rafaga: 40, PrioridadAsignada: 1, Turnaround: 59, TiempoFinalizacion: 2, TiempoFinalizacionReal: 60, Espera: 19),
            new("P3", TiempoLlegada: 2, Rafaga: 30, PrioridadAsignada: 0, Turnaround: 108, TiempoFinalizacion: 4, TiempoFinalizacionReal: 110, Espera: 78),
            new("P1", TiempoLlegada: 0, Rafaga: 50, PrioridadAsignada: 0, Turnaround: 120, TiempoFinalizacion: 6, TiempoFinalizacionReal: 120, Espera: 70),
        ];
        Assert.Equal(esperado, simulador.ObtenerReporte());
        Assert.Equal(120, simulador.ObtenerInstantanea().RelojMs);
        Assert.Equal("abcde", destino.Texto(101));
        Assert.Equal("wxyz", destino.Texto(102));
        Assert.Equal("123", destino.Texto(103));
    }
}
