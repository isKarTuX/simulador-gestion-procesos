using Microsoft.AspNetCore.Mvc;
using SimuladorProcesos.Core.Contrato;

namespace SimuladorProcesos.Api.Controllers;

/// <summary>M4. Stubs con las rutas del contrato (CONTEXT §5.2). Implementa: agente backend.</summary>
[ApiController]
[Route("api/catalogos")]
[Produces("application/xml")]
public sealed class CatalogosController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<CatalogosXml>(StatusCodes.Status200OK)]
    public ActionResult<CatalogosXml> Listar() => PendienteDeImplementar();

    [HttpGet("{id:int}/procesos")]
    [ProducesResponseType<CatalogoXml>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorXml>(StatusCodes.Status404NotFound)]
    public ActionResult<CatalogoXml> Procesos(int id) => PendienteDeImplementar();

    private ObjectResult PendienteDeImplementar() =>
        StatusCode(StatusCodes.Status501NotImplemented, new ErrorXml { Codigo = 501, Mensaje = "Pendiente: módulo M4." });
}
