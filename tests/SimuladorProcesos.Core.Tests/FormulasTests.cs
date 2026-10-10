namespace SimuladorProcesos.Core.Tests;

public class FormulasTests
{
    [Theory]
    [InlineData(100, 14, 1400)]
    [InlineData(1, 0, 0)]
    [InlineData(250, 33, 8250)]
    public void Rafaga_es_TH_por_caracteres(int th, int caracteres, long esperado) =>
        Assert.Equal(esperado, Formulas.Rafaga(th, caracteres));

    [Fact]
    public void Rafaga_rechaza_TH_no_positivo() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Formulas.Rafaga(0, 10));

    [Fact]
    public void TiempoLlegada_es_cero_si_Listo_esta_vacio() =>
        Assert.Equal(0, Formulas.TiempoLlegada(listoVacio: true, tiempoLlegadaUltimo: 7));

    [Fact]
    public void TiempoLlegada_es_el_ultimo_mas_uno_si_Listo_tiene_procesos() =>
        Assert.Equal(8, Formulas.TiempoLlegada(listoVacio: false, tiempoLlegadaUltimo: 7));

    [Fact]
    public void TiempoFinalizacionEnunciado_es_quantum_por_ejecuciones() =>
        Assert.Equal(20, Formulas.TiempoFinalizacionEnunciado(quantum: 5, ejecuciones: 4));

    [Fact]
    public void Turnaround_y_espera_se_derivan_de_TF_TL_y_R()
    {
        var tat = Formulas.Turnaround(tiempoFinalizacion: 35, tiempoLlegada: 0);

        Assert.Equal(35, tat);
        Assert.Equal(25, Formulas.Espera(tat, rafaga: 10));
    }
}
