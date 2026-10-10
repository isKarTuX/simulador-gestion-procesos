using Microsoft.AspNetCore.Mvc;
using SimuladorProcesos.Core.Contrato;

namespace SimuladorProcesos.Api.Controllers;

/// <summary>M4. Datos del algoritmo de planificación de un catálogo. Implementa: agente backend.</summary>
[ApiController]
[Route("api/planificacion")]
[Produces("application/xml")]
public sealed class PlanificacionController : ControllerBase
{
    [HttpGet("{id:int}")]
    [ProducesResponseType<PlanificacionXml>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorXml>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorXml>(StatusCodes.Status404NotFound)]
    public ActionResult<PlanificacionXml> Obtener(int id, [FromQuery] int th = 100, [FromQuery] int quantum = 5) =>
        StatusCode(StatusCodes.Status501NotImplemented, new ErrorXml { Codigo = 501, Mensaje = "Pendiente: módulo M4." });
}
