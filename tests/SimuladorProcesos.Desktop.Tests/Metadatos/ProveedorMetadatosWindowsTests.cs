using System.Diagnostics;
using SimuladorProcesos.Core.Modelos;
using SimuladorProcesos.Core.Reglas;
using SimuladorProcesos.Desktop.Servicios.Metadatos;
using Xunit.Abstractions;

namespace SimuladorProcesos.Desktop.Tests.Metadatos;

public class ProveedorMetadatosWindowsTests(ITestOutputHelper salida)
{
    private readonly ProveedorMetadatosWindows _proveedor = new();

    [Fact]
    public void El_proceso_actual_devuelve_su_usuario_y_una_descripcion()
    {
        using var actual = Process.GetCurrentProcess();

        var metadatos = _proveedor.Obtener(Environment.ProcessId, actual.ProcessName);

        Assert.NotNull(metadatos);
        // Windows no distingue mayúsculas en cuentas ni dominios.
        Assert.Equal($"{Environment.UserDomainName}\\{Environment.UserName}", metadatos.Usuario, ignoreCase: true);
        Assert.False(string.IsNullOrWhiteSpace(metadatos.Descripcion));
    }

    [Fact]
    public void Un_pid_inexistente_devuelve_null()
    {
        Assert.Null(_proveedor.Obtener(int.MaxValue, "fantasma"));
    }

    [Fact]
    public void El_proceso_System_no_lanza_y_es_no_expulsivo()
    {
        var metadatos = _proveedor.Obtener(4, "System");

        Assert.NotNull(metadatos);
        Assert.Equal(Prioridad.NoExpulsivo, ReglaPrioridad.Determinar(metadatos.Usuario));
        // MainModule de System se niega: la descripción cae al nombre recibido, nunca queda vacía.
        Assert.False(string.IsNullOrWhiteSpace(metadatos.Descripcion));
    }

    [Theory]
    [InlineData(0u, "MAQUINA", "ana", "MAQUINA\\ana")]
    [InlineData(0u, "", "ana", "ana")]
    [InlineData(0u, null, "ana", "ana")]
    [InlineData(2u, "MAQUINA", "ana", "")]
    [InlineData(0u, "MAQUINA", "", "")]
    [InlineData(0u, "MAQUINA", null, "")]
    public void ComponerUsuario_arma_dominio_y_cuenta(uint codigo, string? dominio, string? usuario, string esperado)
    {
        Assert.Equal(esperado, ProveedorMetadatosWindows.ComponerUsuario(codigo, dominio, usuario));
    }

    [Fact]
    [Trait("Category", "Medicion")]
    public void Mide_cuanto_tardan_20_consultas_WMI()
    {
        // Informativa: la cifra queda en la salida de la prueba (trx) para decidir si hace falta caché.
        var candidatos = Process.GetProcesses();
        var pids = candidatos.Select(p => (p.Id, p.ProcessName)).Take(20).ToList();
        foreach (var p in candidatos)
        {
            p.Dispose();
        }

        var reloj = Stopwatch.StartNew();
        var resueltos = pids.Count(p => _proveedor.Obtener(p.Id, p.ProcessName) is not null);
        reloj.Stop();

        salida.WriteLine($"{pids.Count} consultas ({resueltos} resueltas) en {reloj.ElapsedMilliseconds} ms");
        Assert.NotEmpty(pids);
    }
}
