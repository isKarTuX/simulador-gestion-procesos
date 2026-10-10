using SimuladorProcesos.Core.Contrato;

namespace SimuladorProcesos.Web.Servicios;

/// <summary>Única vía de la Web a los catálogos: el servicio REST XML de la App 1 (ADR-0001).</summary>
public interface IClienteCatalogos
{
    Task<CatalogosXml> ListarAsync(CancellationToken cancelacion = default);

    Task<CatalogoXml> ObtenerProcesosAsync(int catalogoId, CancellationToken cancelacion = default);

    Task<PlanificacionXml> ObtenerPlanificacionAsync(int catalogoId, int thMs, int quantum, CancellationToken cancelacion = default);
}

/// <summary>La Api respondió un &lt;Error&gt; o no está disponible.</summary>
public sealed class ErrorServicioException(int codigo, string mensaje) : Exception(mensaje)
{
    public int Codigo { get; } = codigo;
}
