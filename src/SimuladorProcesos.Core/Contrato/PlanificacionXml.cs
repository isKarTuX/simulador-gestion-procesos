using System.Xml.Serialization;

namespace SimuladorProcesos.Core.Contrato;

/// <summary>Respuesta de GET /api/planificacion/{id}. Ver docs/contrato/planificacion.xml.</summary>
[XmlRoot("Planificacion")]
public sealed class PlanificacionXml
{
    [XmlAttribute("catalogoId")]
    public int CatalogoId { get; set; }

    [XmlAttribute("algoritmo")]
    public string Algoritmo { get; set; } = "RoundRobin";

    public int TH { get; set; }

    public int Quantum { get; set; }

    [XmlElement("Proceso")]
    public List<ProcesoPlanificacionXml> Procesos { get; set; } = [];
}

public sealed class ProcesoPlanificacionXml
{
    [XmlAttribute("pid")]
    public int Pid { get; set; }

    [XmlAttribute("nombre")]
    public string Nombre { get; set; } = string.Empty;

    [XmlAttribute("caracteres")]
    public int Caracteres { get; set; }

    /// <summary>R = TH × caracteres.</summary>
    [XmlAttribute("rafagaMs")]
    public long RafagaMs { get; set; }

    [XmlAttribute("prioridad")]
    public int Prioridad { get; set; }
}
