using SimuladorProcesos.Core.Modelos;

namespace SimuladorProcesos.Desktop.ViewModels;

/// <summary>Elemento del combo de criterio: el valor del enum y el texto que ve el usuario.</summary>
public sealed record OpcionCriterio(CriterioSeleccion Valor, string Texto)
{
    public static string TextoDe(CriterioSeleccion criterio) => criterio switch
    {
        CriterioSeleccion.Cpu => "CPU",
        CriterioSeleccion.Memoria => "Memoria",
        _ => criterio.ToString(),
    };
}
