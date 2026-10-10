using System.Net.Http.Headers;
using System.Xml;
using SimuladorProcesos.Core.Contrato;

namespace SimuladorProcesos.Web.Servicios;

/// <summary>HttpClient + XmlSerializer contra la Api; la Web nunca toca la BD (ADR-0001).</summary>
public sealed class ClienteCatalogos(HttpClient http) : IClienteCatalogos
{
    private readonly HttpClient _http = http;

    public Task<CatalogosXml> ListarAsync(CancellationToken cancelacion = default) =>
        PedirAsync<CatalogosXml>("api/catalogos", cancelacion);

    public Task<CatalogoXml> ObtenerProcesosAsync(int catalogoId, CancellationToken cancelacion = default) =>
        PedirAsync<CatalogoXml>($"api/catalogos/{catalogoId}/procesos", cancelacion);

    public Task<PlanificacionXml> ObtenerPlanificacionAsync(int catalogoId, int thMs, int quantum, CancellationToken cancelacion = default) =>
        PedirAsync<PlanificacionXml>($"api/planificacion/{catalogoId}?th={thMs}&quantum={quantum}", cancelacion);

    private async Task<T> PedirAsync<T>(string ruta, CancellationToken cancelacion)
    {
        using var peticion = new HttpRequestMessage(HttpMethod.Get, ruta);
        peticion.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));

        string cuerpo;
        try
        {
            using var respuesta = await _http.SendAsync(peticion, cancelacion).ConfigureAwait(false);
            cuerpo = await respuesta.Content.ReadAsStringAsync(cancelacion).ConfigureAwait(false);

            if (!respuesta.IsSuccessStatusCode)
            {
                throw ErrorDe((int)respuesta.StatusCode, cuerpo);
            }
        }
        catch (HttpRequestException)
        {
            // Sin conexión no hay código HTTP: se informa como 503 para que la UI lo trate como "Api apagada".
            throw new ErrorServicioException(503, $"El servicio REST no está disponible en {_http.BaseAddress}");
        }

        try
        {
            return SerializadorXml.Deserializar<T>(cuerpo);
        }
        catch (Exception ex) when (ex is InvalidOperationException or XmlException)
        {
            throw new ErrorServicioException(502, $"La respuesta del servicio REST no tiene el formato esperado ({typeof(T).Name}).");
        }
    }

    // Prefiere el <Error> de la Api; si el cuerpo no es un <Error> válido, cae a un mensaje genérico con el status.
    private static ErrorServicioException ErrorDe(int status, string cuerpo)
    {
        try
        {
            var error = SerializadorXml.Deserializar<ErrorXml>(cuerpo);
            return new ErrorServicioException(error.Codigo > 0 ? error.Codigo : status, error.Mensaje);
        }
        catch (Exception ex) when (ex is InvalidOperationException or XmlException)
        {
            return new ErrorServicioException(status, $"El servicio REST respondió con el estado {status}.");
        }
    }
}
