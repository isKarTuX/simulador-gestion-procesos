using SimuladorProcesos.Desktop.Servicios;

namespace SimuladorProcesos.Desktop.Tests;

/// <summary>Las subtareas 1.3–1.6 agregan sus pruebas en carpetas propias (Recolector/, Metadatos/, Actividad/, ViewModels/).</summary>
public class ContratosServiciosTests
{
    [Fact]
    public void Los_metadatos_se_comparan_por_valor() =>
        Assert.Equal(new MetadatosProceso("SYSTEM", "Kernel"), new MetadatosProceso("SYSTEM", "Kernel"));
}
