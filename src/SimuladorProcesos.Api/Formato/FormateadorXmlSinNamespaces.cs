using System.Xml;
using System.Xml.Serialization;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace SimuladorProcesos.Api.Formato;

/// <summary>Escribe el XML igual que los ejemplos de docs/contrato: sin xmlns:xsi ni xmlns:xsd en la raíz.</summary>
public sealed class FormateadorXmlSinNamespaces : XmlSerializerOutputFormatter
{
    private static readonly XmlSerializerNamespaces _sinNamespaces = new([XmlQualifiedName.Empty]);

    // Con declaración <?xml ...?> como en los ejemplos (el formateador por defecto la omite).
    public FormateadorXmlSinNamespaces()
        : base(new XmlWriterSettings { OmitXmlDeclaration = false, CloseOutput = false, CheckCharacters = false })
    {
    }

    protected override void Serialize(XmlSerializer xmlSerializer, XmlWriter xmlWriter, object? value) =>
        xmlSerializer.Serialize(xmlWriter, value, _sinNamespaces);
}
