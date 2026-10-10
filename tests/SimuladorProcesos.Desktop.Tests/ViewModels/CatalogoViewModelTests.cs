using SimuladorProcesos.Core.Modelos;
using SimuladorProcesos.Desktop.Falsos;
using SimuladorProcesos.Desktop.ViewModels;

namespace SimuladorProcesos.Desktop.Tests.ViewModels;

public class CatalogoViewModelTests
{
    private readonly RepositorioEnMemoria _repositorio = new();

    // Sin latencia ni carpeta real: las pruebas no esperan ni tocan el disco.
    private CatalogoViewModel CrearViewModel() =>
        new(new RecolectorFalso(TimeSpan.Zero), _repositorio, new ActividadFalsa(), "carpeta-de-prueba");

    private static async Task<CatalogoViewModel> IniciadoConCapturaAsync(CatalogoViewModel vm, int cantidad = 5)
    {
        await vm.InicializarAsync();
        vm.Cantidad = cantidad;
        await vm.CapturarCommand.EjecutarAsync();
        return vm;
    }

    [Fact]
    public async Task Al_iniciar_sugiere_el_siguiente_numero_y_carga_los_catalogos()
    {
        await _repositorio.GuardarAsync(new Catalogo { Numero = 7, Nombre = "previo", Procesos = [] });
        var vm = CrearViewModel();

        await vm.InicializarAsync();

        Assert.Equal(8, vm.Numero);
        var guardado = Assert.Single(vm.CatalogosGuardados);
        Assert.Equal(7, guardado.Numero);
        Assert.False(vm.SinCatalogos);
    }

    [Fact]
    public async Task Capturar_llena_la_lista_con_la_cantidad_pedida()
    {
        var vm = CrearViewModel();
        await vm.InicializarAsync();
        vm.Cantidad = 6;

        await vm.CapturarCommand.EjecutarAsync();

        Assert.Equal(6, vm.Procesos.Count);
        Assert.True(vm.HayProcesos);
        Assert.False(vm.MensajeEsError);
        Assert.False(vm.Ocupado);
    }

    [Fact]
    public async Task Capturar_por_cpu_y_por_memoria_da_listas_distintas()
    {
        var vm = CrearViewModel();
        vm.Cantidad = 5;
        vm.Criterio = CriterioSeleccion.Cpu;
        await vm.CapturarCommand.EjecutarAsync();
        var porCpu = vm.Procesos.Select(p => p.Nombre).ToList();

        vm.Criterio = CriterioSeleccion.Memoria;
        await vm.CapturarCommand.EjecutarAsync();
        var porMemoria = vm.Procesos.Select(p => p.Nombre).ToList();

        Assert.NotEqual(porCpu, porMemoria);
    }

    [Fact]
    public async Task Las_filas_muestran_la_prioridad_con_su_significado()
    {
        var vm = await IniciadoConCapturaAsync(CrearViewModel(), 12);

        var delSistema = vm.Procesos.First(p => p.Usuario.Contains("SYSTEM"));
        var delUsuario = vm.Procesos.First(p => p.Usuario.Contains("estudiante"));

        Assert.Equal("1 · No expulsivo", delSistema.Prioridad);
        Assert.Equal("0 · Expulsivo", delUsuario.Prioridad);
    }

    [Fact]
    public async Task Cantidad_invalida_muestra_error_y_deshabilita_capturar()
    {
        var vm = CrearViewModel();
        await vm.InicializarAsync();

        vm.Cantidad = 0;

        Assert.NotEmpty(vm.ErrorCantidad);
        Assert.False(vm.CapturarCommand.CanExecute(null));

        vm.Cantidad = null;
        Assert.NotEmpty(vm.ErrorCantidad);

        vm.Cantidad = 3;
        Assert.Empty(vm.ErrorCantidad);
        Assert.True(vm.CapturarCommand.CanExecute(null));
    }

    [Fact]
    public async Task Numero_menor_o_igual_a_cero_muestra_error()
    {
        var vm = CrearViewModel();
        await vm.InicializarAsync();
        vm.UsarConsecutivo = false;

        vm.Numero = 0;

        Assert.NotEmpty(vm.ErrorNumero);
        Assert.False(vm.FormularioValido);
    }

    [Fact]
    public async Task Guardar_con_nombre_vacio_no_guarda_y_muestra_error()
    {
        var vm = await IniciadoConCapturaAsync(CrearViewModel());
        vm.NombreCatalogo = "   ";

        Assert.NotEmpty(vm.ErrorNombre);
        Assert.False(vm.GuardarCommand.CanExecute(null));

        // Aunque alguien lo ejecute saltándose el botón, tampoco debe guardar.
        await vm.GuardarCommand.EjecutarAsync();

        Assert.Empty(await _repositorio.ListarAsync());
        Assert.True(vm.MensajeEsError);
        Assert.Contains("nombre", vm.Mensaje, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Guardar_sin_capturar_antes_esta_deshabilitado()
    {
        var vm = CrearViewModel();
        await vm.InicializarAsync();
        vm.NombreCatalogo = "demo";

        Assert.False(vm.GuardarCommand.CanExecute(null));
    }

    [Fact]
    public async Task Numero_repetido_muestra_el_mensaje_del_repositorio_y_no_duplica()
    {
        await _repositorio.GuardarAsync(new Catalogo { Numero = 3, Nombre = "existente", Procesos = [] });
        var vm = await IniciadoConCapturaAsync(CrearViewModel());
        vm.UsarConsecutivo = false;
        vm.Numero = 3;
        vm.NombreCatalogo = "repetido";

        Assert.True(vm.GuardarCommand.CanExecute(null));
        await vm.GuardarCommand.EjecutarAsync();

        Assert.True(vm.MensajeEsError);
        Assert.Contains("3", vm.Mensaje);
        Assert.Single(await _repositorio.ListarAsync());
        Assert.False(vm.Ocupado);
    }

    [Fact]
    public async Task Guardar_bien_recarga_la_lista_y_sugiere_el_siguiente_numero()
    {
        var vm = await IniciadoConCapturaAsync(CrearViewModel(), 4);
        Assert.Equal(1, vm.Numero);
        vm.NombreCatalogo = "  Mi primer catálogo  ";

        await vm.GuardarCommand.EjecutarAsync();

        var enBd = Assert.Single(await _repositorio.ListarAsync());
        Assert.Equal("Mi primer catálogo", enBd.Nombre);
        Assert.Equal(1, enBd.Numero);
        Assert.Equal(CriterioSeleccion.Cpu, enBd.Criterio);
        Assert.Equal(4, enBd.Cantidad);

        var panel = Assert.Single(vm.CatalogosGuardados);
        Assert.Equal(1, panel.Numero);
        Assert.Same(panel, vm.CatalogoSeleccionado);
        Assert.Equal(2, vm.Numero);
        Assert.False(vm.MensajeEsError);
        Assert.False(vm.GuardarCommand.CanExecute(null));
    }

    [Fact]
    public async Task Seleccionar_un_catalogo_guardado_muestra_sus_procesos()
    {
        var vm = await IniciadoConCapturaAsync(CrearViewModel(), 3);
        vm.NombreCatalogo = "uno";
        await vm.GuardarCommand.EjecutarAsync();
        vm.Cantidad = 7;
        await vm.CapturarCommand.EjecutarAsync();
        Assert.Equal(7, vm.Procesos.Count);

        await vm.MostrarCatalogoGuardadoAsync(vm.CatalogosGuardados[0]);

        Assert.Equal(3, vm.Procesos.Count);
        Assert.False(vm.GuardarCommand.CanExecute(null));
    }

    [Fact]
    public async Task Volver_a_consecutivo_recupera_el_numero_sugerido()
    {
        var vm = CrearViewModel();
        await vm.InicializarAsync();
        vm.UsarConsecutivo = false;
        vm.Numero = 99;

        vm.UsarConsecutivo = true;

        Assert.Equal(1, vm.Numero);
        Assert.False(vm.NumeroEditable);
    }

    [Fact]
    public async Task Ejecutar_actividad_lleva_todas_las_filas_al_cien_por_ciento()
    {
        var vm = await IniciadoConCapturaAsync(CrearViewModel(), 4);
        vm.ThMs = 1;

        await vm.EjecutarActividadCommand.EjecutarAsync();

        Assert.All(vm.Procesos, fila => Assert.Equal(100, fila.ProgresoActividad));
        Assert.False(vm.MensajeEsError);
        Assert.False(vm.Ocupado);
    }

    [Fact]
    public async Task Cancelar_detiene_la_actividad_y_libera_la_ventana()
    {
        var vm = await IniciadoConCapturaAsync(CrearViewModel(), 3);
        vm.ThMs = 60_000;

        var actividad = vm.EjecutarActividadCommand.EjecutarAsync();
        Assert.True(vm.Ocupado);
        Assert.True(vm.CancelarCommand.CanExecute(null));
        vm.CancelarCommand.Execute(null);
        await actividad;

        Assert.False(vm.Ocupado);
        Assert.Contains("cancel", vm.Mensaje, StringComparison.OrdinalIgnoreCase);
        Assert.All(vm.Procesos, fila => Assert.True(fila.ProgresoActividad < 100));
    }

    [Fact]
    public async Task Th_invalido_muestra_error_y_deshabilita_la_actividad()
    {
        var vm = await IniciadoConCapturaAsync(CrearViewModel(), 2);

        vm.ThMs = 0;

        Assert.NotEmpty(vm.ErrorTh);
        Assert.False(vm.EjecutarActividadCommand.CanExecute(null));
    }
}
