namespace SimuladorProcesos.Engine.Tests;

public class ConfiguracionSimulacionTests
{
    [Fact]
    public void Acepta_TH_y_quantum_positivos()
    {
        var configuracion = new ConfiguracionSimulacion(thMs: 100, quantum: 5);

        Assert.Equal(100, configuracion.ThMs);
        Assert.Equal(5, configuracion.Quantum);
    }

    [Theory]
    [InlineData(0, 5)]
    [InlineData(100, 0)]
    [InlineData(-1, 5)]
    public void Rechaza_valores_no_positivos(int th, int quantum) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new ConfiguracionSimulacion(th, quantum));
}
