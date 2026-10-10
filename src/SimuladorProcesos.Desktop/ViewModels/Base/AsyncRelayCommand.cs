using System.Windows.Input;

namespace SimuladorProcesos.Desktop.ViewModels.Base;

/// <summary>Comando asíncrono: se deshabilita mientras corre para que un doble clic no lo lance dos veces.</summary>
public sealed class AsyncRelayCommand : ICommand
{
    private readonly Func<Task> _ejecutar;
    private readonly Func<bool>? _puedeEjecutar;
    private readonly Action<Exception>? _alFallar;
    private bool _enEjecucion;

    public AsyncRelayCommand(Func<Task> ejecutar, Func<bool>? puedeEjecutar = null, Action<Exception>? alFallar = null)
    {
        _ejecutar = ejecutar;
        _puedeEjecutar = puedeEjecutar;
        _alFallar = alFallar;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => !_enEjecucion && (_puedeEjecutar?.Invoke() ?? true);

    // async void es inevitable aquí (así es ICommand): por eso la excepción se entrega a _alFallar y no se pierde.
    public async void Execute(object? parameter)
    {
        try
        {
            await EjecutarAsync();
        }
        catch (Exception ex) when (_alFallar is not null)
        {
            _alFallar(ex);
        }
    }

    /// <summary>Versión que devuelve la Task: la usan las pruebas y quien necesite esperar el final.</summary>
    public async Task EjecutarAsync()
    {
        if (_enEjecucion)
        {
            return;
        }

        _enEjecucion = true;
        RaiseCanExecuteChanged();
        try
        {
            await _ejecutar();
        }
        finally
        {
            _enEjecucion = false;
            RaiseCanExecuteChanged();
        }
    }

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
