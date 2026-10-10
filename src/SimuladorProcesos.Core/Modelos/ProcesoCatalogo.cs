namespace SimuladorProcesos.Core.Modelos;

/// <summary>Proceso del SO guardado dentro de un catálogo (tabla ProcesoCatalogo).</summary>
public sealed class ProcesoCatalogo
{
    public int Id { get; set; }

    public int CatalogoId { get; set; }

    public int Pid { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string Usuario { get; set; } = string.Empty;

    public string Descripcion { get; set; } = string.Empty;

    public Prioridad Prioridad { get; set; }
}
