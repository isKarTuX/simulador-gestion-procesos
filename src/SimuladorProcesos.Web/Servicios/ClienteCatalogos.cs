using SimuladorProcesos.Core.Contrato;

namespace SimuladorProcesos.Web.Servicios;

/// <summary>HttpClient + XmlSerializer contra la Api. Pendiente: subtarea 1.12 (docs/PROMPTS.md).</summary>
public sealed class ClienteCatalogos(HttpClient http) : IClienteCatalogos
{
    private readonly HttpClient _http = http;

    public Task<CatalogosXml> ListarAsync(CancellationToken cancelacion = default) =>
        throw Pendiente();

    public Task<CatalogoXml> ObtenerProcesosAsync(int catalogoId, CancellationToken cancelacion = default) =>
        throw Pendiente();

    public Task<PlanificacionXml> ObtenerPlanificacionAsync(int catalogoId, int thMs, int quantum, CancellationToken cancelacion = default) =>
        throw Pendiente();

    private NotImplementedException Pendiente() => new($"Subtarea 1.12 (Api en {_http.BaseAddress})");
}
