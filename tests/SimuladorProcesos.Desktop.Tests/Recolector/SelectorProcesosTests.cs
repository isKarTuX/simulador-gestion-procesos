using SimuladorProcesos.Core.Modelos;
using SimuladorProcesos.Desktop.Servicios.Recolector;

namespace SimuladorProcesos.Desktop.Tests.Recolector;

public class SelectorProcesosTests
{
    private static MuestraProceso PorMemoria(int pid, long? bytes) => new(pid, $"p{pid}", bytes);

    private static MuestraProceso PorCpu(int pid, double? inicialMs, double? finalMs) => new(
        pid,
        $"p{pid}",
        null,
        inicialMs is null ? null : TimeSpan.FromMilliseconds(inicialMs.Value),
        finalMs is null ? null : TimeSpan.FromMilliseconds(finalMs.Value));

    [Fact]
    public void Por_memoria_ordena_de_mayor_a_menor()
    {
        var resultado = SelectorProcesos.Ordenar(
            [PorMemoria(1, 100), PorMemoria(2, 300), PorMemoria(3, 200)],
            CriterioSeleccion.Memoria);

        Assert.Equal([2, 3, 1], resultado.Select(m => m.Pid));
    }

    [Fact]
    public void Por_cpu_ordena_por_la_diferencia_entre_las_dos_lecturas_y_no_por_el_total()
    {
        // El pid 1 tiene el mayor acumulado, pero el pid 2 es quien más CPU usó entre las muestras.
        var resultado = SelectorProcesos.Ordenar(
            [PorCpu(1, 9000, 9010), PorCpu(2, 10, 410), PorCpu(3, 0, 100)],
            CriterioSeleccion.Cpu);

        Assert.Equal([2, 3, 1], resultado.Select(m => m.Pid));
    }

    [Fact]
    public void Por_memoria_descarta_las_muestras_sin_dato()
    {
        var resultado = SelectorProcesos.Ordenar(
            [PorMemoria(1, null), PorMemoria(2, 50)],
            CriterioSeleccion.Memoria);

        Assert.Equal([2], resultado.Select(m => m.Pid));
    }

    [Theory]
    [InlineData(null, 100.0)]
    [InlineData(100.0, null)]
    [InlineData(null, null)]
    public void Por_cpu_descarta_las_muestras_con_alguna_lectura_faltante(double? inicial, double? final)
    {
        var resultado = SelectorProcesos.Ordenar(
            [PorCpu(1, inicial, final), PorCpu(2, 0, 10)],
            CriterioSeleccion.Cpu);

        Assert.Equal([2], resultado.Select(m => m.Pid));
    }

    [Fact]
    public void Un_dato_de_otro_criterio_no_cuenta()
    {
        // Una muestra solo con memoria no sirve para ordenar por CPU, y viceversa.
        Assert.Empty(SelectorProcesos.Ordenar([PorMemoria(1, 100)], CriterioSeleccion.Cpu));
        Assert.Empty(SelectorProcesos.Ordenar([PorCpu(1, 0, 10)], CriterioSeleccion.Memoria));
    }

    [Fact]
    public void Por_memoria_desempata_por_pid_ascendente()
    {
        var resultado = SelectorProcesos.Ordenar(
            [PorMemoria(30, 100), PorMemoria(10, 100), PorMemoria(20, 100)],
            CriterioSeleccion.Memoria);

        Assert.Equal([10, 20, 30], resultado.Select(m => m.Pid));
    }

    [Fact]
    public void Por_cpu_desempata_por_pid_ascendente()
    {
        var resultado = SelectorProcesos.Ordenar(
            [PorCpu(30, 0, 50), PorCpu(10, 100, 150), PorCpu(20, 5, 55)],
            CriterioSeleccion.Cpu);

        Assert.Equal([10, 20, 30], resultado.Select(m => m.Pid));
    }

    [Fact]
    public void Sin_muestras_devuelve_una_lista_vacia() =>
        Assert.Empty(SelectorProcesos.Ordenar([], CriterioSeleccion.Memoria));

    [Fact]
    public void Un_criterio_desconocido_lanza_ArgumentOutOfRangeException() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => SelectorProcesos.Ordenar([], (CriterioSeleccion)99));
}
