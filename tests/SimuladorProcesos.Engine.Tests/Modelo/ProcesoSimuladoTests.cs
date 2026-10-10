using SimuladorProcesos.Core.Modelos;
using SimuladorProcesos.Engine.Modelo;

namespace SimuladorProcesos.Engine.Tests.Modelo;

public class ProcesoSimuladoTests
{
    private static readonly ConfiguracionSimulacion _configuracion = new(thMs: 100, quantum: 5);

    [Fact]
    public void Calcula_rafaga_y_usa_el_quantum_global()
    {
        var proceso = new ProcesoSimulado(1, new ProcesoEntrada(10, "WINWORD", "Microsoft Word", Prioridad.Expulsivo), _configuracion);

        Assert.Equal(1400, proceso.Rafaga);
        Assert.Equal(5, proceso.Quantum);
        Assert.Equal(14, proceso.CaracteresRestantes);
        Assert.False(proceso.Completo);
    }

    [Fact]
    public void El_quantum_propio_reemplaza_al_global() =>
        Assert.Equal(8, new ProcesoSimulado(1, new ProcesoEntrada(1, "a", "abc", Prioridad.Expulsivo, QuantumPropio: 8), _configuracion).Quantum);

    [Fact]
    public void Esta_en_espera_solo_si_esta_listo_y_ya_uso_la_CPU()
    {
        var proceso = new ProcesoSimulado(1, new ProcesoEntrada(1, "a", "abc", Prioridad.Expulsivo), _configuracion);
        Assert.False(proceso.EnEspera);

        proceso.Ejecuciones = 1;
        Assert.True(proceso.EnEspera);

        proceso.Estado = EstadoProceso.Ejecucion;
        Assert.False(proceso.EnEspera);
    }
}
