using System.Xml.Serialization;

namespace SimuladorProcesos.Core.Contrato;

/// <summary>Cuerpo de cualquier respuesta de error del servicio. Ver docs/contrato/error.xml.</summary>
[XmlRoot("Error")]
public sealed class ErrorXml
{
    [XmlAttribute("codigo")]
    public int Codigo { get; set; }

    public string Mensaje { get; set; } = string.Empty;
}
