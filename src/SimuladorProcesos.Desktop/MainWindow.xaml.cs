using System.Windows;
using SimuladorProcesos.Desktop.Falsos;
using SimuladorProcesos.Desktop.ViewModels;

namespace SimuladorProcesos.Desktop;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // 2.2 reemplaza los falsos por servicios reales
        var modelo = new CatalogoViewModel(new RecolectorFalso(), new RepositorioEnMemoria(), new ActividadFalsa());

        DataContext = modelo;
        Loaded += async (_, _) => await modelo.InicializarAsync();
    }
}
