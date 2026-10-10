using System.Diagnostics;
using SimuladorProcesos.Core.Modelos;
using SimuladorProcesos.Engine.Tests.Planificacion;
using static SimuladorProcesos.Engine.Tests.Concurrencia.AuxiliarSimulador;

namespace SimuladorProcesos.Engine.Tests.Concurrencia.QA;

/// <summary>
/// QA adversarial: qué ve el resto del mundo mientras Reiniciar espera (hasta 2 s) a un hilo bloqueado en el destino.
/// El destino bloquea la primera escritura para alargar esa ventana de forma determinista.
/// </summary>
[Trait("Category", "Concurrencia")]
public class VentanaDeReinicioTests
{
    private static readonly TimeSpan _espera = TimeSpan.FromSeconds(10);

    // Un expulsivo cuyo primer carácter queda copiado pero sin escribir: su hilo está dentro del destino.
    private static SimuladorRoundRobin ArrancarBloqueado(DestinoBloqueable destino, int caracteres)
    {
        ProcesoEntrada[] procesos = [AuxiliarPlanificador.Proceso("Ana", Texto(caracteres), Prioridad.Expulsivo)];
        var simulador = Cargado(destino, procesos, 50);
        simulador.Iniciar();
        Assert.True(destino.Entro.Wait(_espera), "El proceso no llegó a escribir.");
        return simulador;
    }

    // Lanza Reiniciar en un hilo y deja pasar 300 ms: ya está dentro de Terminar esperando al hilo bloqueado.
    private static Thread ReiniciarEnOtroHilo(SimuladorRoundRobin simulador)
    {
        var reinicio = new Thread(simulador.Reiniciar);
        reinicio.Start();
        Thread.Sleep(300);
        Assert.True(reinicio.IsAlive, "Reiniciar ya había terminado: la ventana no existió y la prueba no prueba nada.");
        return reinicio;
    }

    [Fact]
    public void H1_Pausar_durante_Reiniciar_no_lanza_NullReferenceException()
    {
        var destino = new DestinoBloqueable();
        var simulador = ArrancarBloqueado(destino, 50);
        Exception? lanzada = null;
        var reinicio = ReiniciarEnOtroHilo(simulador);
        try
        {
            simulador.Pausar();
        }
        catch (Exception ex)
        {
            lanzada = ex;
        }
        finally
        {
            destino.Compuerta.Set();
        }

        Assert.True(reinicio.Join(_espera), "Reiniciar no terminó tras liberar el destino.");
        simulador.Dispose();
        Assert.IsNotType<NullReferenceException>(lanzada);
    }

    [Fact]
    public void H1_Reanudar_durante_Reiniciar_no_lanza_NullReferenceException()
    {
        var destino = new DestinoBloqueable();
        var simulador = ArrancarBloqueado(destino, 50);
        simulador.Pausar();
        Exception? lanzada = null;
        var reinicio = ReiniciarEnOtroHilo(simulador);
        try
        {
            simulador.Reanudar();
        }
        catch (Exception ex)
        {
            lanzada = ex;
        }
        finally
        {
            destino.Compuerta.Set();
        }

        Assert.True(reinicio.Join(_espera), "Reiniciar no terminó tras liberar el destino.");
        simulador.Dispose();
        Assert.IsNotType<NullReferenceException>(lanzada);
    }

    [Fact]
    public void H3_Un_hilo_viejo_que_sale_tarde_del_destino_no_ensucia_la_corrida_nueva()
    {
        var destino = new DestinoBloqueable();
        var simulador = ArrancarBloqueado(destino, 6);
        var viejos = simulador.Hilos;
        var pid = (int)'A';
        var cronometro = Stopwatch.StartNew();

        // El hilo del proceso está colgado en el destino: Terminar agota su plazo de 2 s y Reiniciar sigue de largo.
        simulador.Reiniciar();
        var tardo = cronometro.Elapsed;
        Assert.True(tardo.TotalMilliseconds >= 1900, "Reiniciar no esperó el plazo: la prueba no creó un hilo huérfano.");
        Assert.True(viejos.Any(h => h.IsAlive), "Ningún hilo viejo sigue vivo: la prueba no creó un huérfano.");

        // Corrida nueva completa mientras el huérfano sigue dentro de Escribir.
        EjecutarHastaElFinal(simulador);
        Assert.Equal("abcdef", destino.Texto(pid));

        // El huérfano termina su escritura: su carácter 'a' pertenece a una corrida que ya no existe.
        destino.Compuerta.Set();
        Assert.All(viejos, h => Assert.True(h.Join(_espera), "Un hilo viejo no terminó tras liberar el destino."));
        Assert.Equal("abcdef", destino.Texto(pid));
        simulador.Dispose();
    }

    [Fact]
    public void Un_Reiniciar_lento_no_pisa_la_corrida_que_otro_hilo_inicio_despues()
    {
        var destino = new DestinoBloqueable();
        var simulador = ArrancarBloqueado(destino, 400);
        using var fin = new ManualResetEventSlim();
        simulador.SimulacionFinalizada += (_, _) => fin.Set();
        var lento = ReiniciarEnOtroHilo(simulador);

        // Un segundo Reiniciar no encuentra corrida que cancelar, termina al instante, y el usuario vuelve a Iniciar.
        simulador.Reiniciar();
        simulador.Iniciar();

        // Ahora el Reiniciar lento recupera el control: ya no debería tocar nada de la corrida nueva.
        destino.Compuerta.Set();
        Assert.True(lento.Join(_espera), "El Reiniciar lento no terminó.");
        var estadoTrasElLento = simulador.Estado;
        Assert.True(fin.Wait(Limite), "La corrida nueva no terminó.");
        var estadoFinal = simulador.Estado;
        var filas = simulador.ObtenerReporte().Count;
        simulador.Dispose();

        Assert.NotEqual(EstadoSimulacion.Detenida, estadoTrasElLento);
        Assert.Equal(EstadoSimulacion.Finalizada, estadoFinal);
        Assert.Equal(1, filas);
    }
}
