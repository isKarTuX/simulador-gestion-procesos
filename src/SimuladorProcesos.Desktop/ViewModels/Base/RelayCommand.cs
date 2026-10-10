using System.Windows.Input;

namespace SimuladorProcesos.Desktop.ViewModels.Base;

/// <summary>Comando síncrono: el botón llama a Execute y consulta CanExecute para habilitarse.</summary>
public sealed class RelayCommand : ICommand
{
    private readonly Action _ejecutar;
    private readonly Func<bool>? _puedeEjecutar;

    public RelayCommand(Action ejecutar, Func<bool>? puedeEjecutar = null)
    {
        _ejecutar = ejecutar;
        _puedeEjecutar = puedeEjecutar;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => _puedeEjecutar?.Invoke() ?? true;

    public void Execute(object? parameter) => _ejecutar();

    /// <summary>El ViewModel lo llama cuando cambia algo de lo que depende CanExecute; no usamos CommandManager para no atarnos a WPF.</summary>
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
