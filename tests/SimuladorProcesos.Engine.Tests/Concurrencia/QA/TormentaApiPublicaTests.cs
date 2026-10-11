using SimuladorProcesos.Core.Modelos;
using SimuladorProcesos.Engine.Actividad;
using SimuladorProcesos.Engine.Tests.Planificacion;
using static SimuladorProcesos.Engine.Tests.Concurrencia.AuxiliarSimulador;

namespace SimuladorProcesos.Engine.Tests.Concurrencia.QA;

/// <summary>
/// QA adversarial (segunda vuelta): todos los hilos golpean TODA la API pública a la vez mientras corre.
/// Cubre lo que la prueba de 2.1 no tocaba: Cargar/Iniciar concurrentes y lectores de instantánea/reporte/Estado bajo presión.
/// </summary>
[Trait("Category", "Concurrencia")]
public class TormentaApiPublicaTests
{
    private static readonly TimeSpan _duracion = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan _espera = TimeSpan.FromSeconds(30);

    [Fact]
    public void Toda_la_API_bajo_tormenta_concurrente_solo_devuelve_lo_documentado_y_la_corrida_sigue_exacta()
    {
        // Textos cortos a propósito: la tormenta ya presiona con 6 hilos y la corrida final debe caber
        // holgada en el límite aunque el equipo esté cargado (las demás pruebas de Concurrencia corren en paralelo).
        var procesos = new[]
        {
            AuxiliarPlanificador.Proceso("Ana", Texto(250), Prioridad.Expulsivo),
            AuxiliarPlanificador.Proceso("Beto", Texto(250), Prioridad.NoExpulsivo),
            AuxiliarPlanificador.Proceso("Carla", Texto(250), Prioridad.Expulsivo),
        };
        var (reporteEsperado, _) = ReferenciaSinHilos(procesos, 10);
        var destino = new DestinoEnMemoria();
        using var simulador = Cargado(destino, procesos, 10);
        using var fin = new ManualResetEventSlim();
        simulador.SimulacionFinalizada += (_, _) => fin.Set();
        simulador.Iniciar();

        var inesperadas = new List<string>();
        var intentos = new Dictionary<string, int>();
        void Anotar(string operacion, Exception? error)
        {
            lock (inesperadas)
            {
                intentos[operacion] = intentos.GetValueOrDefault(operacion) + 1;
                if (error is not null)
                {
                    inesperadas.Add($"{operacion}: {error.GetType().Name}: {error.Message}");
                }
            }
        }

        // Cargar siempre trae el MISMO catálogo: aunque alguna entre en Detenida/Finalizada, el reporte final sigue siendo determinista.
        var configuracion = new ConfiguracionSimulacion(ThMs, 10);
        var atacantes = new[]
        {
            new Thread(() => Operar("Cargar", () => simulador.Cargar(procesos, configuracion), Anotar)),
            new Thread(() => Operar("Iniciar", simulador.Iniciar, Anotar)),
            new Thread(() => Operar("Pausar", simulador.Pausar, Anotar)),
            new Thread(() => Operar("Reanudar", simulador.Reanudar, Anotar)),
            new Thread(() => Operar("Reiniciar", simulador.Reiniciar, Anotar)),
            new Thread(() => Operar("Lectores", () =>
            {
                _ = simulador.Estado;
                _ = simulador.ObtenerInstantanea();
                _ = simulador.ObtenerReporte();
            }, Anotar, soloDocumentadas: false)),
        };
        foreach (var atacante in atacantes)
        {
            atacante.Start();
        }

        Assert.All(atacantes, h => Assert.True(h.Join(_espera), "Un hilo de la tormenta se colgó."));
        Assert.Empty(inesperadas);
        // Basta con que cada operación haya corrido: un umbral de vueltas depende de la velocidad del equipo y daba falsos fallos.
        foreach (var operacion in new[] { "Cargar", "Iniciar", "Pausar", "Reanudar", "Reiniciar", "Lectores" })
        {
            Assert.True(intentos.GetValueOrDefault(operacion) > 0, $"La tormenta no presionó {operacion}: 0 intentos.");
        }

        // Pase lo que pase arriba, el catálogo cargado es el mismo: Reiniciar y correr da el reporte exacto.
        simulador.Reiniciar();
        EjecutarHastaElFinal(simulador);

        Assert.Equal(reporteEsperado, simulador.ObtenerReporte());
        Assert.All(procesos, p => Assert.Equal(p.Descripcion, destino.Texto(p.Pid)));

        // Join acotado en vez de IsAlive: un hilo que acaba de terminar puede seguir marcado vivo unos instantes y eso no es una fuga.
        Assert.All(simulador.Hilos, h => Assert.True(h.Join(_espera), $"El hilo {h.Name} no terminó."));
    }

    // Las operaciones de control solo pueden responder InvalidOperationException (llegaron en mal estado);
    // los lectores no pueden lanzar nada (el simulador no se libera durante la tormenta).
    private static void Operar(string operacion, Action accion, Action<string, Exception?> anotar, bool soloDocumentadas = true)
    {
        var limite = DateTime.UtcNow + _duracion;
        while (DateTime.UtcNow < limite)
        {
            try
            {
                accion();
                anotar(operacion, null);
            }
            catch (InvalidOperationException) when (soloDocumentadas)
            {
                anotar(operacion, null);
            }
            catch (Exception ex)
            {
                anotar(operacion, ex);
                return;
            }
        }
    }
}
