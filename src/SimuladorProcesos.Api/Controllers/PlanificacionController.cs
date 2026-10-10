using Microsoft.AspNetCore.Mvc;
using SimuladorProcesos.Api.Errores;
using SimuladorProcesos.Api.Mapeo;
using SimuladorProcesos.Core.Contrato;
using SimuladorProcesos.Data;

namespace SimuladorProcesos.Api.Controllers;

/// <summary>M4. Datos del algoritmo de planificación de un catálogo (TH, quantum y ráfaga por proceso).</summary>
[ApiController]
[Route("api/planificacion")]
[Produces("application/xml")]
public sealed class PlanificacionController(ICatalogoRepositorio repositorio) : ControllerBase
{
    [HttpGet("{id:int}")]
    [ProducesResponseType<PlanificacionXml>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorXml>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorXml>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlanificacionXml>> Obtener(
        int id, [FromQuery] int th = 100, [FromQuery] int quantum = 5, CancellationToken cancelacion = default)
    {
        // Se valida antes de consultar la BD: un 400 no necesita saber si el catálogo existe.
        if (th <= 0 || quantum <= 0)
        {
            return RespuestasError.Crear(
                StatusCodes.Status400BadRequest, $"th y quantum deben ser mayores que cero (th={th}, quantum={quantum}).");
        }

        var catalogo = await repositorio.ObtenerConProcesosAsync(id, cancelacion);
        if (catalogo is null)
        {
            return RespuestasError.Crear(StatusCodes.Status404NotFound, $"No existe el catálogo con id {id}.");
        }

        return catalogo.APlanificacionXml(th, quantum);
    }
}
