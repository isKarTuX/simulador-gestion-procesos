using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SimuladorProcesos.Desktop.ViewModels.Base;

/// <summary>Base de todo ViewModel: avisa a la vista cuando una propiedad cambia para que se vuelva a leer.</summary>
public abstract class ObservableObject : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Asigna el campo y notifica solo si el valor realmente cambió (evita bucles de enlace).</summary>
    protected bool SetProperty<T>(ref T campo, T valor, [CallerMemberName] string? propiedad = null)
    {
        if (EqualityComparer<T>.Default.Equals(campo, valor))
        {
            return false;
        }

        campo = valor;
        OnPropertyChanged(propiedad);
        return true;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propiedad = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propiedad));
}
