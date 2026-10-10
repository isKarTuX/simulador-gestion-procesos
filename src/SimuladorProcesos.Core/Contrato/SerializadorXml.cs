using System.Xml.Serialization;

namespace SimuladorProcesos.Core.Contrato;

/// <summary>Serializa y deserializa los DTO del contrato con XmlSerializer, igual que la Api.</summary>
public static class SerializadorXml
{
    public static T Deserializar<T>(string xml)
    {
        var serializador = new XmlSerializer(typeof(T));
        using var lector = new StringReader(xml);
        return (T)(serializador.Deserialize(lector)
            ?? throw new InvalidOperationException($"El XML no contiene un {typeof(T).Name}."));
    }

    public static string Serializar<T>(T valor)
    {
        var serializador = new XmlSerializer(typeof(T));
        using var escritor = new StringWriter();
        serializador.Serialize(escritor, valor);
        return escritor.ToString();
    }
}
