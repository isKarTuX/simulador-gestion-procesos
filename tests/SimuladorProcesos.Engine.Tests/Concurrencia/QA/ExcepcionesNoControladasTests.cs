using System.Diagnostics;
using System.Runtime.InteropServices;
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
    private const string _variable = "QA_ESCENARIO";

    private static ProcesoEntrada[] Procesos() =>
    [
        AuxiliarPlanificador.Proceso("Ana", Texto(50), Prioridad.Expulsivo),
        AuxiliarPlanificador.Proceso("Beto", Texto(50), Prioridad.NoExpulsivo),
    ];

    [Fact]
    public void Disparador_handler_que_lanza()
    {
        if (Environment.GetEnvironmentVariable(_variable) != "handler")
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
        if (Environment.GetEnvironmentVariable(_variable) != "destino")
        {
            return;
        }

        using var simulador = Cargado(new DestinoQueLanza(), Procesos(), 5);
        simulador.Iniciar();
        Thread.Sleep(3000);
    }

    private static void Fallar() => throw new ObjectDisposedException("Circuito Blazor ya cerrado (simulado por QA).");

    // El ejecutable "dotnet" que corre esta prueba, hallado desde el runtime en uso: no depende del PATH ni del sistema operativo.
    private static string RutaDotnet()
    {
        var nombre = OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet";
        var desdeRuntime = Path.GetFullPath(Path.Combine(RuntimeEnvironment.GetRuntimeDirectory(), "..", "..", "..", nombre));
        return File.Exists(desdeRuntime) ? desdeRuntime : "dotnet";
    }

    [Theory]
    [InlineData("handler")]
    [InlineData("destino")]
    [Trait("Category", "Concurrencia")]
    public async Task Lanzador_una_excepcion_en_un_hilo_del_motor_no_debe_derribar_el_host(string escenario)
    {
        var inicio = new ProcessStartInfo(RutaDotnet())
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        inicio.ArgumentList.Add("test");
        inicio.ArgumentList.Add(typeof(ExcepcionesNoControladasTests).Assembly.Location);
        inicio.ArgumentList.Add("--filter");
        inicio.ArgumentList.Add("FullyQualifiedName~Disparador_" + escenario);
        inicio.Environment[_variable] = escenario;

        using var hijo = Process.Start(inicio)!;
        var salidaEstandar = hijo.StandardOutput.ReadToEndAsync();
        var salidaError = hijo.StandardError.ReadToEndAsync();
        if (!hijo.WaitForExit(180_000))
        {
            hijo.Kill(entireProcessTree: true);
            Assert.Fail("El host hijo no terminó.");
        }

        var texto = await salidaEstandar + await salidaError;
        salida.WriteLine(texto);

        // Esperado: el motor contiene la falla y el host hijo termina bien. Obtenido: el host se aborta.
        Assert.DoesNotContain("crashed", texto, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, hijo.ExitCode);
    }
}
