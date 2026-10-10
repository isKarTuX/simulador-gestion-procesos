using System.Xml.Serialization;

namespace SimuladorProcesos.Core.Contrato;

/// <summary>Respuesta de GET /api/catalogos. Ver docs/contrato/catalogos.xml.</summary>
[XmlRoot("Catalogos")]
public sealed class CatalogosXml
{
    [XmlElement("Catalogo")]
    public List<CatalogoResumenXml> Catalogos { get; set; } = [];
}

/// <summary>Un catálogo sin sus procesos.</summary>
public sealed class CatalogoResumenXml
{
    [XmlAttribute("id")]
    public int Id { get; set; }

    [XmlAttribute("numero")]
    public int Numero { get; set; }

    [XmlAttribute("nombre")]
    public string Nombre { get; set; } = string.Empty;

    [XmlAttribute("fecha")]
    public DateTime Fecha { get; set; }

    /// <summary>"CPU" o "MEMORIA".</summary>
    [XmlAttribute("criterio")]
    public string Criterio { get; set; } = string.Empty;

    [XmlAttribute("cantidad")]
    public int Cantidad { get; set; }
}
