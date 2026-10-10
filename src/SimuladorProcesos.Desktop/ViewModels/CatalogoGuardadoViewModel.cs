using SimuladorProcesos.Core.Modelos;

namespace SimuladorProcesos.Desktop.ViewModels;

/// <summary>Elemento del panel lateral: resumen de un catálogo ya guardado.</summary>
public sealed class CatalogoGuardadoViewModel
{
    public CatalogoGuardadoViewModel(Catalogo catalogo)
    {
        Id = catalogo.Id;
        Numero = catalogo.Numero;
        Titulo = $"#{catalogo.Numero} · {catalogo.Nombre}";
        Detalle = $"{OpcionCriterio.TextoDe(catalogo.Criterio)} · {catalogo.Cantidad} procesos · {catalogo.Fecha:dd/MM/yyyy HH:mm}";
    }

    public int Id { get; }

    public int Numero { get; }

    public string Titulo { get; }

    public string Detalle { get; }
}
