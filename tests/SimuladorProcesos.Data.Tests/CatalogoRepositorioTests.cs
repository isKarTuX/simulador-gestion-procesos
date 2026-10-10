using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SimuladorProcesos.Core.Modelos;

namespace SimuladorProcesos.Data.Tests;

public class CatalogoRepositorioTests
{
    private static Catalogo NuevoCatalogo(int numero, int procesos = 2) => new()
    {
        Numero = numero,
        Nombre = $"Catálogo {numero}",
        Fecha = new DateTime(2026, 10, 10, 8, 0, 0),
        Criterio = CriterioSeleccion.Cpu,
        Cantidad = procesos,
        Procesos = Enumerable.Range(1, procesos)
            .Select(i => new ProcesoCatalogo
            {
                Pid = 100 + i,
                Nombre = $"proc{i}",
                Usuario = "usuario",
                Descripcion = $"Proceso {i}",
                Prioridad = Prioridad.NoExpulsivo,
            })
            .ToList(),
    };

    private static async Task<Catalogo> GuardarEnContextoNuevoAsync(BaseDatosEnMemoria bd, Catalogo catalogo)
    {
        await using var contexto = bd.CrearContexto();
        return await new CatalogoRepositorio(contexto).GuardarAsync(catalogo);
    }

    // ---- ListarAsync ----

    [Fact]
    public async Task Listar_devuelve_los_catalogos_ordenados_por_numero_sin_procesos()
    {
        using var bd = new BaseDatosEnMemoria();
        await GuardarEnContextoNuevoAsync(bd, NuevoCatalogo(3));
        await GuardarEnContextoNuevoAsync(bd, NuevoCatalogo(1));
        await GuardarEnContextoNuevoAsync(bd, NuevoCatalogo(2));

        await using var contexto = bd.CrearContexto();
        var lista = await new CatalogoRepositorio(contexto).ListarAsync();

        Assert.Equal([1, 2, 3], lista.Select(c => c.Numero));
        Assert.All(lista, c => Assert.Empty(c.Procesos));
        Assert.Empty(contexto.ChangeTracker.Entries());
    }

    [Fact]
    public async Task Listar_con_la_bd_vacia_devuelve_lista_vacia()
    {
        using var bd = new BaseDatosEnMemoria();
        await using var contexto = bd.CrearContexto();

        var lista = await new CatalogoRepositorio(contexto).ListarAsync();

        Assert.Empty(lista);
    }

    [Fact]
    public async Task Listar_con_cancelacion_previa_lanza_OperationCanceledException()
    {
        using var bd = new BaseDatosEnMemoria();
        await using var contexto = bd.CrearContexto();
        using var fuente = new CancellationTokenSource();
        await fuente.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => new CatalogoRepositorio(contexto).ListarAsync(fuente.Token));
    }

    // ---- ObtenerConProcesosAsync ----

    [Fact]
    public async Task Obtener_devuelve_el_catalogo_con_sus_procesos_en_orden_de_captura()
    {
        using var bd = new BaseDatosEnMemoria();
        var guardado = await GuardarEnContextoNuevoAsync(bd, NuevoCatalogo(1, procesos: 3));

        await using var contexto = bd.CrearContexto();
        var catalogo = await new CatalogoRepositorio(contexto).ObtenerConProcesosAsync(guardado.Id);

        Assert.NotNull(catalogo);
        Assert.Equal(1, catalogo.Numero);
        Assert.Equal(CriterioSeleccion.Cpu, catalogo.Criterio);
        Assert.Equal([101, 102, 103], catalogo.Procesos.Select(p => p.Pid));
        Assert.Equal(Prioridad.NoExpulsivo, catalogo.Procesos[0].Prioridad);
    }

    [Fact]
    public async Task Obtener_no_mezcla_los_procesos_de_otros_catalogos()
    {
        using var bd = new BaseDatosEnMemoria();
        var primero = await GuardarEnContextoNuevoAsync(bd, NuevoCatalogo(1, procesos: 1));
        await GuardarEnContextoNuevoAsync(bd, NuevoCatalogo(2, procesos: 4));

        await using var contexto = bd.CrearContexto();
        var catalogo = await new CatalogoRepositorio(contexto).ObtenerConProcesosAsync(primero.Id);

        Assert.Single(catalogo!.Procesos);
    }

    [Fact]
    public async Task Obtener_un_id_inexistente_devuelve_null()
    {
        using var bd = new BaseDatosEnMemoria();
        await GuardarEnContextoNuevoAsync(bd, NuevoCatalogo(1));

        await using var contexto = bd.CrearContexto();
        var catalogo = await new CatalogoRepositorio(contexto).ObtenerConProcesosAsync(999);

        Assert.Null(catalogo);
    }

    // ---- GuardarAsync ----

    [Fact]
    public async Task Guardar_devuelve_el_catalogo_con_id_y_persiste_sus_procesos()
    {
        using var bd = new BaseDatosEnMemoria();

        var guardado = await GuardarEnContextoNuevoAsync(bd, NuevoCatalogo(5, procesos: 2));

        Assert.True(guardado.Id > 0);
        await using var contexto = bd.CrearContexto();
        var leido = await new CatalogoRepositorio(contexto).ObtenerConProcesosAsync(guardado.Id);
        Assert.Equal(2, leido!.Procesos.Count);
        Assert.All(leido.Procesos, p => Assert.Equal(guardado.Id, p.CatalogoId));
    }

    [Fact]
    public async Task Guardar_conserva_la_fecha_cuando_viene_informada()
    {
        using var bd = new BaseDatosEnMemoria();
        var fecha = new DateTime(2026, 1, 2, 3, 4, 5);
        var catalogo = NuevoCatalogo(1);
        catalogo.Fecha = fecha;

        var guardado = await GuardarEnContextoNuevoAsync(bd, catalogo);

        Assert.Equal(fecha, guardado.Fecha);
    }

    [Fact]
    public async Task Guardar_asigna_la_fecha_actual_cuando_viene_en_default()
    {
        using var bd = new BaseDatosEnMemoria();
        var catalogo = NuevoCatalogo(1);
        catalogo.Fecha = default;
        var antes = DateTime.Now;

        var guardado = await GuardarEnContextoNuevoAsync(bd, catalogo);

        Assert.InRange(guardado.Fecha, antes, DateTime.Now);
    }

    [Fact]
    public async Task Guardar_con_numero_repetido_lanza_InvalidOperationException_en_espanol()
    {
        using var bd = new BaseDatosEnMemoria();
        await GuardarEnContextoNuevoAsync(bd, NuevoCatalogo(4));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => GuardarEnContextoNuevoAsync(bd, NuevoCatalogo(4)));

        Assert.Contains("4", ex.Message);
        Assert.Contains("catálogo", ex.Message);

        await using var contexto = bd.CrearContexto();
        Assert.Single(await new CatalogoRepositorio(contexto).ListarAsync());
    }

    [Fact]
    public async Task Guardar_traduce_la_colision_de_numero_en_la_bd_a_InvalidOperationException()
    {
        using var bd = new BaseDatosEnMemoria();
        // Simula otra instancia que inserta el mismo número entre la comprobación y el guardado.
        var carrera = new InsertarAntesDeGuardar(() =>
        {
            using var otro = bd.CrearContexto();
            otro.Catalogos.Add(NuevoCatalogo(8, procesos: 1));
            otro.SaveChanges();
        });
        await using var contexto = bd.CrearContexto(carrera);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => new CatalogoRepositorio(contexto).GuardarAsync(NuevoCatalogo(8)));

        Assert.Contains("8", ex.Message);
        Assert.IsType<DbUpdateException>(ex.InnerException);
        Assert.Empty(contexto.ChangeTracker.Entries());
    }

    private sealed class InsertarAntesDeGuardar(Action insertar) : SaveChangesInterceptor
    {
        private bool _hecho;

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> resultado, CancellationToken cancelacion = default)
        {
            if (!_hecho)
            {
                _hecho = true;
                insertar();
            }

            return ValueTask.FromResult(resultado);
        }
    }

    [Fact]
    public async Task Guardar_con_numero_repetido_no_deja_procesos_huerfanos()
    {
        using var bd = new BaseDatosEnMemoria();
        await GuardarEnContextoNuevoAsync(bd, NuevoCatalogo(4, procesos: 2));
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => GuardarEnContextoNuevoAsync(bd, NuevoCatalogo(4, procesos: 3)));

        await using var contexto = bd.CrearContexto();
        Assert.Equal(2, contexto.Procesos.Count());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Guardar_con_nombre_vacio_lanza_ArgumentException(string nombre)
    {
        using var bd = new BaseDatosEnMemoria();
        var catalogo = NuevoCatalogo(1);
        catalogo.Nombre = nombre;

        await Assert.ThrowsAsync<ArgumentException>(() => GuardarEnContextoNuevoAsync(bd, catalogo));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public async Task Guardar_con_cantidad_no_positiva_lanza_ArgumentException(int cantidad)
    {
        using var bd = new BaseDatosEnMemoria();
        var catalogo = NuevoCatalogo(1);
        catalogo.Cantidad = cantidad;

        await Assert.ThrowsAsync<ArgumentException>(() => GuardarEnContextoNuevoAsync(bd, catalogo));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Guardar_con_numero_no_positivo_lanza_ArgumentException(int numero)
    {
        using var bd = new BaseDatosEnMemoria();

        await Assert.ThrowsAsync<ArgumentException>(() => GuardarEnContextoNuevoAsync(bd, NuevoCatalogo(numero)));
    }

    [Fact]
    public async Task Guardar_un_catalogo_nulo_lanza_ArgumentNullException()
    {
        using var bd = new BaseDatosEnMemoria();
        await using var contexto = bd.CrearContexto();

        await Assert.ThrowsAsync<ArgumentNullException>(() => new CatalogoRepositorio(contexto).GuardarAsync(null!));
    }

    [Fact]
    public async Task Guardar_invalido_no_persiste_nada()
    {
        using var bd = new BaseDatosEnMemoria();
        var catalogo = NuevoCatalogo(1);
        catalogo.Nombre = "";
        await Assert.ThrowsAsync<ArgumentException>(() => GuardarEnContextoNuevoAsync(bd, catalogo));

        await using var contexto = bd.CrearContexto();
        Assert.Empty(await new CatalogoRepositorio(contexto).ListarAsync());
    }

    // ---- SiguienteNumeroAsync ----

    [Fact]
    public async Task SiguienteNumero_con_la_bd_vacia_es_1()
    {
        using var bd = new BaseDatosEnMemoria();
        await using var contexto = bd.CrearContexto();

        Assert.Equal(1, await new CatalogoRepositorio(contexto).SiguienteNumeroAsync());
    }

    [Fact]
    public async Task SiguienteNumero_es_el_maximo_mas_uno_aunque_haya_huecos()
    {
        using var bd = new BaseDatosEnMemoria();
        await GuardarEnContextoNuevoAsync(bd, NuevoCatalogo(2));
        await GuardarEnContextoNuevoAsync(bd, NuevoCatalogo(9));
        await GuardarEnContextoNuevoAsync(bd, NuevoCatalogo(5));

        await using var contexto = bd.CrearContexto();

        Assert.Equal(10, await new CatalogoRepositorio(contexto).SiguienteNumeroAsync());
    }

    [Fact]
    public async Task SiguienteNumero_con_cancelacion_previa_lanza_OperationCanceledException()
    {
        using var bd = new BaseDatosEnMemoria();
        await using var contexto = bd.CrearContexto();
        using var fuente = new CancellationTokenSource();
        await fuente.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => new CatalogoRepositorio(contexto).SiguienteNumeroAsync(fuente.Token));
    }
}
