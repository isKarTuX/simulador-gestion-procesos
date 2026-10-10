using System.Xml.Serialization;

namespace SimuladorProcesos.Core.Contrato;

/// <summary>Respuesta de GET /api/catalogos/{id}/procesos. Ver docs/contrato/catalogo-procesos.xml.</summary>
[XmlRoot("Catalogo")]
public sealed class CatalogoXml
{
    [XmlAttribute("id")]
    public int Id { get; set; }

    [XmlAttribute("numero")]
    public int Numero { get; set; }

    [XmlAttribute("nombre")]
    public string Nombre { get; set; } = string.Empty;

    [XmlAttribute("fecha")]
    public DateTime Fecha { get; set; }

    [XmlAttribute("criterio")]
    public string Criterio { get; set; } = string.Empty;

    [XmlAttribute("cantidad")]
    public int Cantidad { get; set; }

    [XmlElement("Proceso")]
    public List<ProcesoXml> Procesos { get; set; } = [];
}

public sealed class ProcesoXml
{
    public int Pid { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string Usuario { get; set; } = string.Empty;

    public string Descripcion { get; set; } = string.Empty;

    /// <summary>0 = expulsivo, 1 = no expulsivo.</summary>
    public int Prioridad { get; set; }
}
