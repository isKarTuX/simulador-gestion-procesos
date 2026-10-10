namespace SimuladorProcesos.Core.Modelos;

/// <summary>Conjunto de procesos capturados en un momento dado (tabla Catalogo).</summary>
public sealed class Catalogo
{
    public int Id { get; set; }

    /// <summary>Número consecutivo o digitado por el usuario; único.</summary>
    public int Numero { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public DateTime Fecha { get; set; }

    public CriterioSeleccion Criterio { get; set; }

    /// <summary>N: cantidad de procesos pedidos por el usuario.</summary>
    public int Cantidad { get; set; }

    public List<ProcesoCatalogo> Procesos { get; set; } = [];
}
