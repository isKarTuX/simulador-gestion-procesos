using SimuladorProcesos.Core.Modelos;
using SimuladorProcesos.Engine.Actividad;
using SimuladorProcesos.Engine.Tests.Planificacion;
using static SimuladorProcesos.Engine.Tests.Concurrencia.AuxiliarSimulador;

namespace SimuladorProcesos.Engine.Tests.Concurrencia.QA;

/// <summary>QA adversarial: reinicio inmediato y cierre desde dentro de un evento.</summary>
[Trait("Category", "Concurrencia")]
public class ReinicioInmediatoYDisposeTests
{
    private static readonly TimeSpan _espera = TimeSpan.FromSeconds(10);

    [Fact]
    public void Un_reinicio_inmediato_repetido_no_deja_hilos_ni_estado_sucio()
    {
        var datos = Catalogo("mezcla");
        var procesos = datos.Procesos;
        var quantum = datos.Quantum;
        var reporteEsperado = ReferenciaSinHilos(procesos, quantum).Reporte;
        var destino = new DestinoEnMemoria();
        using var simulador = Cargado(destino, procesos, quantum);
        var vuelta = 0;

        while (vuelta != 300)
        {
            simulador.Iniciar();
            var hilos = simulador.Hilos;

            // Retardos de 0 a 4 ms para caer en fases distintas: antes del primer despacho, a mitad de turno, entre turnos.
            Thread.Sleep(Math.Min(vuelta, 4));
            vuelta++;
            simulador.Reiniciar();

            Assert.DoesNotContain(hilos, h => h.IsAlive);
            Assert.Equal(EstadoSimulacion.Detenida, simulador.Estado);
            var foto = simulador.ObtenerInstantanea();
            Assert.Equal(0, foto.RelojMs);
            Assert.Equal(procesos.Length, foto.Listos.Count);
            Assert.Null(foto.Ejecucion);
            Assert.Empty(foto.Terminados);
            Assert.All(foto.Listos, p => Assert.Equal(0, p.Ejecuciones));
            Assert.Empty(simulador.ObtenerReporte());
        }

        EjecutarHastaElFinal(simulador);
        Assert.Equal(reporteEsperado, simulador.ObtenerReporte());
        Assert.All(procesos, p => Assert.Equal(p.Descripcion, destino.Texto(p.Pid)));
    }

    [Fact]
    public void Dispose_llamado_desde_un_handler_de_CambioEstado_no_se_cuelga_ni_deja_hilos()
    {
        using var hecho = new ManualResetEventSlim();
        Exception? error = null;
        IReadOnlyList<Thread> hilos = [];
        var pedidos = 0;
        ProcesoEntrada[] procesos =
        [
            AuxiliarPlanificador.Proceso("Ana", Texto(200), Prioridad.Expulsivo),
            AuxiliarPlanificador.Proceso("Beto", Texto(100), Prioridad.NoExpulsivo),
        ];
        var simulador = Cargado(new DestinoEnMemoria(), procesos, 10);
        simulador.CambioEstado += (_, e) =>
        {
            if (e.Hacia == EstadoProceso.Ejecucion && Interlocked.Increment(ref pedidos) == 1)
            {
                try
                {
                    hilos = simulador.Hilos;
                    simulador.Dispose();
                }
                catch (Exception ex)
                {
                    error = ex;
                }
                finally
                {
                    hecho.Set();
                }
            }
        };

        simulador.Iniciar();

        Assert.True(hecho.Wait(_espera), "Dispose desde el handler se colgó.");
        Assert.Null(error);

        // El hilo del handler es el dispatcher: tras volver del handler debe salir por la cancelación.
        Assert.All(hilos, h => Assert.True(h.Join(_espera), "Un hilo sigue vivo tras Dispose desde el handler."));
        Assert.Throws<ObjectDisposedException>(simulador.Iniciar);
        Assert.Throws<ObjectDisposedException>(simulador.ObtenerInstantanea);
    }

    [Fact]
    public void Dispose_llamado_desde_el_handler_de_SimulacionFinalizada_no_se_cuelga_ni_deja_hilos()
    {
        ProcesoEntrada[] procesos =
        [
            AuxiliarPlanificador.Proceso("Ana", Texto(30), Prioridad.Expulsivo),
            AuxiliarPlanificador.Proceso("Beto", Texto(20), Prioridad.NoExpulsivo),
        ];
        var simulador = Cargado(new DestinoEnMemoria(), procesos, 4);
        using var hecho = new ManualResetEventSlim();
        Exception? error = null;
        simulador.SimulacionFinalizada += (_, _) =>
        {
            try
            {
                simulador.Dispose();
            }
            catch (Exception ex)
            {
                error = ex;
            }
            finally
            {
                hecho.Set();
            }
        };

        simulador.Iniciar();
        var hilos = simulador.Hilos;

        Assert.True(hecho.Wait(_espera), "Dispose desde SimulacionFinalizada se colgó.");
        Assert.Null(error);
        Assert.All(hilos, h => Assert.True(h.Join(_espera), "Un hilo sigue vivo."));
    }

    [Fact]
    public void Dispose_y_Reiniciar_simultaneos_terminan_y_no_dejan_hilos()
    {
        var vuelta = 0;
        var inesperadas = new List<Exception>();
        while (vuelta != 100)
        {
            vuelta++;
            var datos = Catalogo("mezcla");
            var simulador = Cargado(new DestinoEnMemoria(), datos.Procesos, datos.Quantum);
            simulador.Iniciar();
            var hilos = simulador.Hilos;
            var reinicio = new Thread(() => Intentar(simulador.Reiniciar, inesperadas));
            var cierre = new Thread(() => Intentar(simulador.Dispose, inesperadas));
            reinicio.Start();
            cierre.Start();

            Assert.True(reinicio.Join(_espera), "Reiniciar se colgó.");
            Assert.True(cierre.Join(_espera), "Dispose se colgó.");
            Assert.DoesNotContain(hilos, h => h.IsAlive);
        }

        Assert.Empty(inesperadas);
    }

    // Reiniciar tras Dispose lanza ObjectDisposedException: es la respuesta correcta, cualquier otra excepción es un fallo.
    private static void Intentar(Action accion, List<Exception> inesperadas)
    {
        try
        {
            accion();
        }
        catch (ObjectDisposedException)
        {
        }
        catch (Exception ex)
        {
            lock (inesperadas)
            {
                inesperadas.Add(ex);
            }
        }
    }
}
