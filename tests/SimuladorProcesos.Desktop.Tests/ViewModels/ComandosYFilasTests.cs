using SimuladorProcesos.Core.Modelos;
using SimuladorProcesos.Desktop.ViewModels;
using SimuladorProcesos.Desktop.ViewModels.Base;

namespace SimuladorProcesos.Desktop.Tests.ViewModels;

public class ComandosYFilasTests
{
    private sealed class Ejemplo : ObservableObject
    {
        private int _valor;

        public int Valor
        {
            get => _valor;
            set => SetProperty(ref _valor, value);
        }
    }

    [Fact]
    public void SetProperty_notifica_solo_cuando_el_valor_cambia()
    {
        var objeto = new Ejemplo();
        var avisos = new List<string?>();
        objeto.PropertyChanged += (_, e) => avisos.Add(e.PropertyName);

        objeto.Valor = 5;
        objeto.Valor = 5;

        Assert.Equal(["Valor"], avisos);
    }

    [Fact]
    public void RelayCommand_respeta_CanExecute_y_avisa_cuando_cambia()
    {
        var habilitado = false;
        var ejecuciones = 0;
        var comando = new RelayCommand(() => ejecuciones++, () => habilitado);
        var avisos = 0;
        comando.CanExecuteChanged += (_, _) => avisos++;

        Assert.False(comando.CanExecute(null));
        habilitado = true;
        comando.RaiseCanExecuteChanged();
        comando.Execute(null);

        Assert.True(comando.CanExecute(null));
        Assert.Equal(1, ejecuciones);
        Assert.Equal(1, avisos);
    }

    [Fact]
    public async Task AsyncRelayCommand_no_se_puede_lanzar_dos_veces_a_la_vez()
    {
        var liberar = new TaskCompletionSource();
        var ejecuciones = 0;
        var comando = new AsyncRelayCommand(async () =>
        {
            ejecuciones++;
            await liberar.Task;
        });

        var primera = comando.EjecutarAsync();
        Assert.False(comando.CanExecute(null));
        await comando.EjecutarAsync();
        liberar.SetResult();
        await primera;

        Assert.Equal(1, ejecuciones);
        Assert.True(comando.CanExecute(null));
    }

    [Fact]
    public async Task AsyncRelayCommand_vuelve_a_habilitarse_aunque_falle()
    {
        var comando = new AsyncRelayCommand(() => throw new InvalidOperationException("falla"));

        await Assert.ThrowsAsync<InvalidOperationException>(comando.EjecutarAsync);

        Assert.True(comando.CanExecute(null));
    }

    [Fact]
    public void La_fila_recorta_el_progreso_entre_0_y_100()
    {
        var fila = new ProcesoFilaViewModel(new ProcesoCatalogo { Pid = 4, Nombre = "System" });

        fila.ProgresoActividad = 140;
        Assert.Equal(100, fila.ProgresoActividad);

        fila.ProgresoActividad = -3;
        Assert.Equal(0, fila.ProgresoActividad);
    }

    [Theory]
    [InlineData(Prioridad.Expulsivo, "0 · Expulsivo")]
    [InlineData(Prioridad.NoExpulsivo, "1 · No expulsivo")]
    public void La_fila_muestra_la_prioridad_como_numero_y_texto(Prioridad prioridad, string esperado)
    {
        var fila = new ProcesoFilaViewModel(new ProcesoCatalogo { Prioridad = prioridad });

        Assert.Equal(esperado, fila.Prioridad);
    }
}
