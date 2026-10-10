using Microsoft.AspNetCore.Mvc;
using SimuladorProcesos.Api.Errores;
using SimuladorProcesos.Api.Mapeo;
using SimuladorProcesos.Core.Contrato;
using SimuladorProcesos.Data;

namespace SimuladorProcesos.Api.Controllers;

/// <summary>M4. Lectura de catálogos que la App 1 dejó en la BD (CONTEXT §5.2).</summary>
[ApiController]
[Route("api/catalogos")]
[Produces("application/xml")]
public sealed class CatalogosController(ICatalogoRepositorio repositorio) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<CatalogosXml>(StatusCodes.Status200OK)]
    public async Task<ActionResult<CatalogosXml>> Listar(CancellationToken cancelacion)
    {
        var catalogos = await repositorio.ListarAsync(cancelacion);
        return new CatalogosXml { Catalogos = [.. catalogos.Select(c => c.ACatalogoResumenXml())] };
    }

    [HttpGet("{id:int}/procesos")]
    [ProducesResponseType<CatalogoXml>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorXml>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CatalogoXml>> Procesos(int id, CancellationToken cancelacion)
    {
        var catalogo = await repositorio.ObtenerConProcesosAsync(id, cancelacion);
        if (catalogo is null)
        {
            return RespuestasError.Crear(StatusCodes.Status404NotFound, $"No existe el catálogo con id {id}.");
        }

        return catalogo.ACatalogoXml();
    }
}
