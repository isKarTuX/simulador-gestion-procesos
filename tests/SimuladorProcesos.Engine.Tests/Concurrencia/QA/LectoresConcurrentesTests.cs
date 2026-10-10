using SimuladorProcesos.Core.Modelos;
using SimuladorProcesos.Engine.Actividad;
using SimuladorProcesos.Engine.Tests.Planificacion;
using static SimuladorProcesos.Engine.Tests.Concurrencia.AuxiliarSimulador;

namespace SimuladorProcesos.Engine.Tests.Concurrencia.QA;

/// <summary>
/// QA adversarial (segunda vuelta): lectores de instantánea/reporte/Estado en varios hilos mientras corre.
/// Cada foto se tomó bajo el lock del motor, así que debe ser internamente consistente: bandas disjuntas,
/// reloj y terminados monótonos (no hay Reiniciar en esta prueba) y Finalizada solo con todo terminado.
/// </summary>
[Trait("Category", "Concurrencia")]
public class LectoresConcurrentesTests
{
    private static readonly TimeSpan _espera = TimeSpan.FromSeconds(30);

    [Fact]
    public void Las_instantaneas_leidas_en_paralelo_son_consistentes_y_monotonas()
    {
        var procesos = new[]
        {
            AuxiliarPlanificador.Proceso("Ana", Texto(300), Prioridad.Expulsivo),
            AuxiliarPlanificador.Proceso("Beto", Texto(200), Prioridad.Expulsivo),
            AuxiliarPlanificador.Proceso("Carla", Texto(250), Prioridad.NoExpulsivo),
        };
        var nombres = new HashSet<string>(procesos.Select(p => p.Nombre));
        var total = procesos.Length;
        var (reporteEsperado, _) = ReferenciaSinHilos(procesos, 8);
        using var simulador = Cargado(new DestinoEnMemoria(), procesos, 8);
        using var fin = new ManualResetEventSlim();
        simulador.SimulacionFinalizada += (_, _) => fin.Set();
        simulador.Iniciar();

        var violaciones = new List<string>();
        void Violar(string detalle)
        {
            lock (violaciones)
            {
                violaciones.Add(detalle);
            }
        }

        var lectores = Enumerable.Range(0, 4).Select(i => new Thread(() =>
        {
            var relojAnterior = -1L;
            var terminadosAnteriores = -1;
            while (true)
            {
                InstantaneaSimulacion foto;
                IReadOnlyList<FilaReporte> reporte;
                try
                {
                    foto = simulador.ObtenerInstantanea();
                    reporte = simulador.ObtenerReporte();
                    _ = simulador.Estado;
                }
                catch (Exception ex)
                {
                    Violar($"lector {i}: los lectores no deben lanzar: {ex.GetType().Name}");
                    return;
                }

                if (foto.RelojMs < relojAnterior)
                {
                    Violar($"lector {i}: el reloj retrocedió de {relojAnterior} a {foto.RelojMs}.");
                }

                if (foto.Terminados.Count < terminadosAnteriores)
                {
                    Violar($"lector {i}: los terminados decrecieron de {terminadosAnteriores} a {foto.Terminados.Count}.");
                }

                relojAnterior = foto.RelojMs;
                terminadosAnteriores = foto.Terminados.Count;

                var enListos = foto.Listos.Select(p => p.Numero).ToHashSet();
                var enTerminados = foto.Terminados.Select(p => p.Numero).ToHashSet();
                if (foto.Ejecucion is not null && (enListos.Contains(foto.Ejecucion.Numero) || enTerminados.Contains(foto.Ejecucion.Numero)))
                {
                    Violar($"lector {i}: {foto.Ejecucion.Nombre} está en Ejecución y en otra banda a la vez.");
                }

                if (!enListos.IsSupersetOf(foto.Espera.Select(p => p.Numero)))
                {
                    Violar($"lector {i}: la banda Espera trae procesos que no están en Listos.");
                }

                foreach (var nombre in foto.Listos.Select(p => p.Nombre)
                    .Append(foto.Ejecucion?.Nombre)
                    .Concat(foto.Terminados.Select(p => p.Nombre))
                    .Concat(reporte.Select(f => f.Proceso)))
                {
                    if (nombre is not null && !nombres.Contains(nombre))
                    {
                        Violar($"lector {i}: nombre desconocido '{nombre}'.");
                    }
                }

                if (reporte.Count > total)
                {
                    Violar($"lector {i}: el reporte trae {reporte.Count} filas para {total} procesos.");
                }

                if (foto.Estado == EstadoSimulacion.Finalizada)
                {
                    if (foto.Terminados.Count != total || foto.Ejecucion is not null)
                    {
                        Violar($"lector {i}: Finalizada con {foto.Terminados.Count}/{total} terminados o CPU ocupada.");
                    }

                    return;
                }
            }
        })).ToArray();
        foreach (var lector in lectores)
        {
            lector.Start();
        }

        Assert.True(fin.Wait(_espera), "La simulación no terminó.");
        Assert.All(lectores, lector => Assert.True(lector.Join(_espera), "Un lector no vio el fin."));
        Assert.Empty(violaciones);
        Assert.Equal(reporteEsperado, simulador.ObtenerReporte());
    }
}
