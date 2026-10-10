using SimuladorProcesos.Core.Modelos;
using SimuladorProcesos.Desktop.ViewModels.Base;
using PrioridadProceso = SimuladorProcesos.Core.Modelos.Prioridad;

namespace SimuladorProcesos.Desktop.ViewModels;

/// <summary>Una fila del grid: los datos del proceso más el avance de su actividad.</summary>
public sealed class ProcesoFilaViewModel : ObservableObject
{
    private double _progresoActividad;

    public ProcesoFilaViewModel(ProcesoCatalogo modelo) => Modelo = modelo;

    /// <summary>Proceso original; es el que se guarda en la BD y el que recibe la actividad.</summary>
    public ProcesoCatalogo Modelo { get; }

    public int Pid => Modelo.Pid;

    public string Nombre => Modelo.Nombre;

    public string Usuario => Modelo.Usuario;

    public string Descripcion => Modelo.Descripcion;

    /// <summary>El número que se guarda (0/1) y su significado, para que el grid sea autoexplicativo.</summary>
    public string Prioridad => $"{(int)Modelo.Prioridad} · {TextoPrioridad(Modelo.Prioridad)}";

    /// <summary>0 a 100; se recorta para que una barra nunca se salga de su rango.</summary>
    public double ProgresoActividad
    {
        get => _progresoActividad;
        set => SetProperty(ref _progresoActividad, Math.Clamp(value, 0, 100));
    }

    private static string TextoPrioridad(PrioridadProceso prioridad) => prioridad switch
    {
        PrioridadProceso.Expulsivo => "Expulsivo",
        PrioridadProceso.NoExpulsivo => "No expulsivo",
        _ => prioridad.ToString(),
    };
}
