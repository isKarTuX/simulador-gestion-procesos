using System.Diagnostics;
using SimuladorProcesos.Core.Modelos;
using SimuladorProcesos.Engine.Actividad;
using SimuladorProcesos.Engine.Tests.Planificacion;
using Xunit.Abstractions;
using static SimuladorProcesos.Engine.Tests.Concurrencia.AuxiliarSimulador;

namespace SimuladorProcesos.Engine.Tests.Concurrencia.QA;

/// <summary>
/// QA: una excepción sin capturar en un hilo propio del motor derriba el host completo (en la Web, todas las sesiones).
/// Los Disparador solo actúan si la variable QA_ESCENARIO lo pide; el Lanzador los corre en otro host de pruebas.
/// </summary>
[Trait("Category", "QA-Aislado")]
public class ExcepcionesNoControladasTests(ITestOutputHelper salida)
{
    private const string Variable = "QA_ESCENARIO";

    private static ProcesoEntrada[] Procesos() =>
    [
        AuxiliarPlanificador.Proceso("Ana", Texto(50), Prioridad.Expulsivo),
        AuxiliarPlanificador.Proceso("Beto", Texto(50), Prioridad.NoExpulsivo),
    ];

    [Fact]
    public void Disparador_handler_que_lanza()
    {
        if (Environment.GetEnvironmentVariable(Variable) != "handler")
        {
            return;
        }

        using var simulador = Cargado(new DestinoEnMemoria(), Procesos(), 5);
        simulador.CambioEstado += (_, e) =>
        {
            if (e.Hacia == EstadoProceso.Ejecucion)
            {
                Fallar();
            }
        };

        simulador.Iniciar();
        Thread.Sleep(3000);
    }

    [Fact]
    public void Disparador_destino_que_lanza()
    {
        if (Environment.GetEnvironmentVariable(Variable) != "destino")
        {
            return;
        }

        using var simulador = Cargado(new DestinoQueLanza(), Procesos(), 5);
        simulador.Iniciar();
        Thread.Sleep(3000);
    }

    private static void Fallar() => throw new ObjectDisposedException("Circuito Blazor ya cerrado (simulado por QA).");

    [Theory]
    [InlineData("handler")]
    [InlineData("destino")]
    public async Task Lanzador_una_excepcion_en_un_hilo_del_motor_no_debe_derribar_el_host(string escenario)
    {
        var inicio = new ProcessStartInfo("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        inicio.ArgumentList.Add("test");
        inicio.ArgumentList.Add(typeof(ExcepcionesNoControladasTests).Assembly.Location);
        inicio.ArgumentList.Add("--filter");
        inicio.ArgumentList.Add("FullyQualifiedName~Disparador_" + escenario);
        inicio.Environment[Variable] = escenario;

        using var hijo = Process.Start(inicio)!;
        var salidaEstandar = hijo.StandardOutput.ReadToEndAsync();
        var salidaError = hijo.StandardError.ReadToEndAsync();
        Assert.True(hijo.WaitForExit(180_000), "El host hijo no terminó.");
        var texto = await salidaEstandar + await salidaError;
        salida.WriteLine(texto);

        // Esperado: el motor contiene la falla y el host hijo termina bien. Obtenido: el host se aborta.
        Assert.DoesNotContain("crashed", texto, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, hijo.ExitCode);
    }
}
