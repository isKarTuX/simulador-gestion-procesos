using SimuladorProcesos.Core.Modelos;
using SimuladorProcesos.Engine.Actividad;
using SimuladorProcesos.Engine.Tests.Planificacion;
using static SimuladorProcesos.Engine.Tests.Concurrencia.AuxiliarSimulador;

namespace SimuladorProcesos.Engine.Tests.Concurrencia.QA;

/// <summary>
/// QA adversarial (segunda vuelta): Reiniciar desde el handler de SimulacionFinalizada.
/// Ese handler corre en el hilo dispatcher; Reiniciar debe reentrar sin colgarse (no puede unirse a sí mismo)
/// y la corrida siguiente debe dar el reporte exacto. Dispose desde ese handler ya se probó en 2.1; Reiniciar, no.
/// </summary>
[Trait("Category", "Concurrencia")]
public class ReinicioDesdeFinTests
{
    private static readonly TimeSpan _espera = TimeSpan.FromSeconds(10);

    [Fact]
    public void Reiniciar_desde_el_handler_de_SimulacionFinalizada_no_se_cuelga_y_la_corrida_siguiente_es_exacta()
    {
        var procesos = new[]
        {
            AuxiliarPlanificador.Proceso("Ana", Texto(120), Prioridad.Expulsivo),
            AuxiliarPlanificador.Proceso("Beto", Texto(80), Prioridad.NoExpulsivo),
        };
        var (reporteEsperado, _) = ReferenciaSinHilos(procesos, 6);
        var destino = new DestinoEnMemoria();
        using var simulador = Cargado(destino, procesos, 6);
        using var reiniciado = new ManualResetEventSlim();
        using var terminado = new ManualResetEventSlim();
        var avisos = 0;
        Exception? error = null;
        simulador.SimulacionFinalizada += (_, _) =>
        {
            if (Interlocked.Increment(ref avisos) == 1)
            {
                try
                {
                    simulador.Reiniciar();
                }
                catch (Exception ex)
                {
                    error = ex;
                }
                finally
                {
                    reiniciado.Set();
                }
            }
            else
            {
                terminado.Set();
            }
        };

        simulador.Iniciar();
        var hilos = simulador.Hilos;

        Assert.True(reiniciado.Wait(_espera), "Reiniciar desde el handler se colgó.");
        Assert.Null(error);
        Assert.Equal(EstadoSimulacion.Detenida, simulador.Estado);
        Assert.Equal(0, simulador.ObtenerInstantanea().RelojMs);
        Assert.Empty(simulador.ObtenerReporte());
        Assert.DoesNotContain(hilos, h => h.IsAlive);

        EjecutarHastaElFinal(simulador);

        Assert.True(terminado.Wait(_espera), "La segunda corrida no avisó su fin.");
        Assert.Equal(2, Volatile.Read(ref avisos));
        Assert.Equal(reporteEsperado, simulador.ObtenerReporte());
        Assert.All(procesos, p => Assert.Equal(p.Descripcion, destino.Texto(p.Pid)));
        Assert.DoesNotContain(simulador.Hilos, h => h.IsAlive);
    }
}
