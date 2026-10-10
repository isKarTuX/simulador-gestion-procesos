using SimuladorProcesos.Core.Modelos;
using SimuladorProcesos.Desktop.Servicios;
using SimuladorProcesos.Desktop.Servicios.Recolector;

namespace SimuladorProcesos.Desktop.Tests.Recolector;

public class RecolectorProcesosTests
{
    private sealed class LectorFalso(params MuestraProceso[] muestras) : ILectorProcesos
    {
        public Task<IReadOnlyList<MuestraProceso>> LeerAsync(CriterioSeleccion criterio, CancellationToken cancelacion = default) =>
            Task.FromResult<IReadOnlyList<MuestraProceso>>(muestras);
    }

    // Un pid sin entrada en el diccionario se comporta como un proceso que ya terminó.
    private sealed class MetadatosFalsos(Dictionary<int, MetadatosProceso> datos) : IProveedorMetadatos
    {
        public List<int> Consultados { get; } = [];

        public MetadatosProceso? Obtener(int pid, string nombreProceso)
        {
            Consultados.Add(pid);
            return datos.GetValueOrDefault(pid);
        }
    }

    private static MuestraProceso Memoria(int pid, long bytes) => new(pid, $"p{pid}", bytes);

    [Fact]
    public async Task Devuelve_los_N_mayores_con_usuario_descripcion_y_nombre()
    {
        var recolector = new RecolectorProcesos(
            new LectorFalso(Memoria(1, 10), Memoria(2, 30), Memoria(3, 20)),
            new MetadatosFalsos(new()
            {
                [1] = new("ana", "Uno"),
                [2] = new("ana", "Dos"),
                [3] = new("ana", "Tres"),
            }));

        var resultado = await recolector.CapturarAsync(2, CriterioSeleccion.Memoria);

        Assert.Equal([2, 3], resultado.Select(p => p.Pid));
        Assert.Equal("p2", resultado[0].Nombre);
        Assert.Equal("ana", resultado[0].Usuario);
        Assert.Equal("Dos", resultado[0].Descripcion);
    }

    [Fact]
    public async Task Si_los_metadatos_son_null_salta_ese_proceso_y_completa_N()
    {
        var metadatos = new MetadatosFalsos(new()
        {
            [1] = new("ana", "Uno"),
            [3] = new("ana", "Tres"),
            [4] = new("ana", "Cuatro"),
        });
        var recolector = new RecolectorProcesos(
            new LectorFalso(Memoria(1, 10), Memoria(2, 40), Memoria(3, 30), Memoria(4, 20)),
            metadatos);

        var resultado = await recolector.CapturarAsync(2, CriterioSeleccion.Memoria);

        // El pid 2 (el mayor) terminó: se omite y entran el 3 y el 4.
        Assert.Equal([3, 4], resultado.Select(p => p.Pid));
        Assert.DoesNotContain(1, metadatos.Consultados);
    }

    [Fact]
    public async Task Aplica_ReglaPrioridad_segun_el_usuario()
    {
        var recolector = new RecolectorProcesos(
            new LectorFalso(Memoria(1, 30), Memoria(2, 20), Memoria(3, 10)),
            new MetadatosFalsos(new()
            {
                [1] = new("NT AUTHORITY\\SYSTEM", "Sistema"),
                [2] = new("PC\\ana", "App"),
                [3] = new(string.Empty, "Protegido"),
            }));

        var resultado = await recolector.CapturarAsync(3, CriterioSeleccion.Memoria);

        Assert.Equal(Prioridad.NoExpulsivo, resultado[0].Prioridad);
        Assert.Equal(Prioridad.Expulsivo, resultado[1].Prioridad);
        Assert.Equal(Prioridad.NoExpulsivo, resultado[2].Prioridad);
    }

    [Fact]
    public async Task Si_hay_menos_procesos_que_la_cantidad_devuelve_los_que_haya()
    {
        var recolector = new RecolectorProcesos(
            new LectorFalso(Memoria(1, 10), Memoria(2, 20)),
            new MetadatosFalsos(new() { [1] = new("ana", "Uno"), [2] = new("ana", "Dos") }));

        var resultado = await recolector.CapturarAsync(10, CriterioSeleccion.Memoria);

        Assert.Equal(2, resultado.Count);
    }

    [Fact]
    public async Task Ignora_las_muestras_sin_dato_del_criterio_sin_consultar_sus_metadatos()
    {
        var metadatos = new MetadatosFalsos(new() { [2] = new("ana", "Dos") });
        var recolector = new RecolectorProcesos(
            new LectorFalso(new MuestraProceso(1, "p1"), Memoria(2, 20)),
            metadatos);

        var resultado = await recolector.CapturarAsync(2, CriterioSeleccion.Memoria);

        Assert.Equal([2], resultado.Select(p => p.Pid));
        Assert.Equal([2], metadatos.Consultados);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public async Task Una_cantidad_menor_o_igual_a_cero_lanza_ArgumentOutOfRangeException(int cantidad)
    {
        var recolector = new RecolectorProcesos(new LectorFalso(), new MetadatosFalsos([]));

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => recolector.CapturarAsync(cantidad, CriterioSeleccion.Memoria));
    }

    [Fact]
    public async Task Una_cancelacion_previa_detiene_la_captura()
    {
        var recolector = new RecolectorProcesos(
            new LectorFalso(Memoria(1, 10)),
            new MetadatosFalsos(new() { [1] = new("ana", "Uno") }));
        using var cancelacion = new CancellationTokenSource();
        await cancelacion.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => recolector.CapturarAsync(1, CriterioSeleccion.Memoria, cancelacion.Token));
    }
}
