using System.Windows;
using SimuladorProcesos.Desktop.ViewModels;

namespace SimuladorProcesos.Desktop;

public partial class MainWindow : Window
{
    public MainWindow(CatalogoViewModel modelo)
    {
        InitializeComponent();

        // El contenedor de App.xaml.cs entrega el ViewModel ya armado con los servicios reales.
        ArgumentNullException.ThrowIfNull(modelo);

        DataContext = modelo;
        Loaded += async (_, _) => await modelo.InicializarAsync();
    }
}
