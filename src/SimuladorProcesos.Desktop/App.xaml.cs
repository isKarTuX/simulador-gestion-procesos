using System.IO;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using SimuladorProcesos.Core;
using SimuladorProcesos.Data;
using SimuladorProcesos.Desktop.Servicios;
using SimuladorProcesos.Desktop.Servicios.Metadatos;
using SimuladorProcesos.Desktop.Servicios.Recolector;
using SimuladorProcesos.Desktop.ViewModels;

namespace SimuladorProcesos.Desktop;

public partial class App : Application
{
    private ServiceProvider? _proveedor;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += AlFallarEnDispatcher;
        TaskScheduler.UnobservedTaskException += AlFallarTareaSinObservar;

        var servicios = new ServiceCollection();
        servicios.AddSimuladorData();
        servicios.AddSingleton<ILectorProcesos, LectorProcesosSistema>();
        servicios.AddSingleton<IProveedorMetadatos, ProveedorMetadatosWindows>();
        servicios.AddSingleton<IRecolectorProcesos, RecolectorProcesos>();
        servicios.AddSingleton<IActividadProceso, ActividadProceso>();
        servicios.AddTransient<CatalogoViewModel>(proveedor => new CatalogoViewModel(
            proveedor.GetRequiredService<IRecolectorProcesos>(),
            proveedor.GetRequiredService<ICatalogoRepositorio>(),
            proveedor.GetRequiredService<IActividadProceso>(),
            RutasSimulador.CarpetaActividad));
        servicios.AddSingleton<MainWindow>();
        _proveedor = servicios.BuildServiceProvider();

        try
        {
            // La App 1 y la Api comparten la misma BD: se crea aquí para que el primer Guardar no falle.
            using var alcance = _proveedor.CreateScope();
            await InicializadorBaseDatos.AsegurarCreadaAsync(
                alcance.ServiceProvider.GetRequiredService<SimuladorDbContext>());
        }
        catch (Exception ex)
        {
            RegistrarError(ex);
            MessageBox.Show(
                "No se pudo preparar la base de datos. El detalle quedó en el registro de errores de la carpeta de datos.",
                "Simulador de Procesos",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(1);
            return;
        }

        _proveedor.GetRequiredService<MainWindow>().Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        DispatcherUnhandledException -= AlFallarEnDispatcher;
        TaskScheduler.UnobservedTaskException -= AlFallarTareaSinObservar;
        _proveedor?.Dispose();
        base.OnExit(e);
    }

    private void AlFallarEnDispatcher(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        // Sin este Handled la ventana se cerraría de golpe ante cualquier fallo no previsto.
        e.Handled = true;
        RegistrarError(e.Exception);
        MessageBox.Show(
            "Ocurrió un error inesperado, pero la aplicación sigue abierta. El detalle quedó en el registro de errores de la carpeta de datos.",
            "Simulador de Procesos",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
    }

    private void AlFallarTareaSinObservar(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        // SetObserved evita que el fallo de una tarea en segundo plano termine el proceso al liberarla.
        e.SetObserved();
        RegistrarError(e.Exception);
        MessageBox.Show(
            "Una tarea en segundo plano falló. El detalle quedó en el registro de errores de la carpeta de datos.",
            "Simulador de Procesos",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
    }

    private static void RegistrarError(Exception error)
    {
        try
        {
            // El log vive junto a la BD para encontrarlo sin adivinar rutas.
            Directory.CreateDirectory(RutasSimulador.CarpetaDatos);
            var linea = $"[{DateTime.Now:O}] {error.GetType().Name}: {error.Message}{Environment.NewLine}{error.StackTrace}{Environment.NewLine}";
            File.AppendAllText(Path.Combine(RutasSimulador.CarpetaDatos, "errores.log"), linea);
        }
        catch (Exception)
        {
            // El registro nunca debe romper la aplicación que ya está fallando.
        }
    }
}
