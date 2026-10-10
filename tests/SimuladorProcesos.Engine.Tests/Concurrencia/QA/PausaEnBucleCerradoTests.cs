using SimuladorProcesos.Core.Modelos;
using SimuladorProcesos.Engine.Actividad;
using SimuladorProcesos.Engine.Tests.Planificacion;
using Xunit.Abstractions;
using static SimuladorProcesos.Engine.Tests.Concurrencia.AuxiliarSimulador;

namespace SimuladorProcesos.Engine.Tests.Concurrencia.QA;

/// <summary>QA adversarial: Pausar/Reanudar sin descanso mientras los hilos copian.</summary>
[Trait("Category", "Concurrencia")]
public class PausaEnBucleCerradoTests(ITestOutputHelper salida)
{
    private static ProcesoEntrada[] Catalogo3() =>
    [
        AuxiliarPlanificador.Proceso("Ana", Texto(300), Prioridad.Expulsivo),
        AuxiliarPlanificador.Proceso("Beto", Texto(200), Prioridad.NoExpulsivo),
        AuxiliarPlanificador.Proceso("Carla", Texto(250), Prioridad.Expulsivo, quantumPropio: 7),
    ];

    [Fact]
    public void Pausar_y_Reanudar_en_bucle_cerrado_no_lanzan_y_el_resultado_sigue_siendo_exacto()
    {
        var procesos = Catalogo3();
        var (reporteEsperado, _) = ReferenciaSinHilos(procesos, 5);
        var destino = new DestinoEnMemoria();
        using var simulador = Cargado(destino, procesos, 5);
        using var fin = new ManualResetEventSlim();
        simulador.SimulacionFinalizada += (_, _) => fin.Set();
        var errores = new List<Exception>();
        var vueltas = 0;

        var martillo = new Thread(() =>
        {
            while (!fin.IsSet)
            {
                try
                {
                    simulador.Pausar();
                    Thread.Yield();
                    simulador.Reanudar();
                    vueltas++;
                }
                catch (InvalidOperationException) when (simulador.Estado == EstadoSimulacion.Finalizada)
                {
                    // La simulación acabó entre la consulta y la llamada: es la carrera esperada, no un fallo del bucle.
                    break;
                }
                catch (Exception ex)
                {
                    errores.Add(ex);
                    break;
                }
            }
        });

        simulador.Iniciar();
        var hilos = simulador.Hilos;
        martillo.Start();

        Assert.True(fin.Wait(Limite), "La simulación no terminó con Pausar/Reanudar en bucle.");
        Assert.True(martillo.Join(TimeSpan.FromSeconds(10)), "El hilo martillo no terminó.");
        salida.WriteLine("Vueltas Pausar/Reanudar completadas: " + vueltas);

        Assert.Empty(errores);
        Assert.True(vueltas > 50, "La prueba no fue exigente: solo " + vueltas + " vueltas.");
        Assert.Equal(reporteEsperado, simulador.ObtenerReporte());
        Assert.All(procesos, p => Assert.Equal(p.Descripcion, destino.Texto(p.Pid)));
        Assert.DoesNotContain(hilos, h => h.IsAlive);
    }

    [Fact]
    public void Tras_Pausar_el_reloj_no_avanza_y_a_lo_sumo_queda_una_escritura_en_vuelo()
    {
        var procesos = Catalogo3();
        var destino = new DestinoConGancho(escritos => Thread.Yield());
        using var simulador = Cargado(destino, procesos, 5);
        simulador.Iniciar();
        var desfases = 0;
        var i = 0;
        while (i != 60)
        {
            i++;
            Esperar(() => Avanzo(simulador), "El reloj no avanzó.");
            if (simulador.Estado == EstadoSimulacion.Finalizada)
            {
                break;
            }

            simulador.Pausar();
            var reloj0 = simulador.ObtenerInstantanea().RelojMs;
            var escritos0 = destino.Total;
            if (escritos0 != reloj0)
            {
                desfases++;
            }

            Thread.Sleep(5);
            Assert.Equal(reloj0, simulador.ObtenerInstantanea().RelojMs);
            Assert.InRange(destino.Total - escritos0, 0, 1);
            simulador.Reanudar();
            Thread.Sleep(Math.Min(i, 3));
        }

        // Informativo: caracteres copiados (reloj) que aún no se habían escrito cuando Pausar retornó.
        salida.WriteLine("Pausas con una escritura todavía en vuelo al retornar Pausar: " + desfases);
    }

    private static bool Avanzo(SimuladorRoundRobin simulador)
    {
        var foto = simulador.ObtenerInstantanea();
        return foto.RelojMs != 0 || foto.Estado == EstadoSimulacion.Finalizada;
    }
}
