using SimuladorProcesos.Core.Modelos;
using SimuladorProcesos.Desktop.Servicios;
using SimuladorProcesos.Desktop.Servicios.Recolector;

namespace SimuladorProcesos.Desktop.Tests.Recolector;

/// <summary>Pruebas de humo contra el SO real (filtrables con Category=SistemaReal).</summary>
[Trait("Category", "SistemaReal")]
public class LectorProcesosSistemaTests
{
    private sealed class MetadatosFalsos : IProveedorMetadatos
    {
        public MetadatosProceso? Obtener(int pid, string nombreProceso) => new("usuario", nombreProceso);
    }

    [Fact]
    public async Task Captura_5_procesos_reales_por_memoria()
    {
        var recolector = new RecolectorProcesos(new LectorProcesosSistema(), new MetadatosFalsos());

        var resultado = await recolector.CapturarAsync(5, CriterioSeleccion.Memoria);

        Assert.Equal(5, resultado.Count);
        Assert.All(resultado, p =>
        {
            Assert.True(p.Pid > 0);
            Assert.NotEqual(Environment.ProcessId, p.Pid);
            Assert.False(string.IsNullOrWhiteSpace(p.Nombre));
        });
        Assert.Equal(5, resultado.Select(p => p.Pid).Distinct().Count());
    }

    [Fact]
    public async Task Captura_5_procesos_reales_por_cpu()
    {
        var recolector = new RecolectorProcesos(
            new LectorProcesosSistema(TimeSpan.FromMilliseconds(100)),
            new MetadatosFalsos());

        var resultado = await recolector.CapturarAsync(5, CriterioSeleccion.Cpu);

        Assert.Equal(5, resultado.Count);
    }
}
