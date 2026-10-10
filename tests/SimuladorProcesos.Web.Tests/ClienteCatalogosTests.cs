using System.Net;
using System.Text;
using SimuladorProcesos.Core.Modelos;
using SimuladorProcesos.Web.Servicios;

namespace SimuladorProcesos.Web.Tests;

/// <summary>Prueba ClienteCatalogos sin la Api real: un handler falso responde con los ejemplos de docs/contrato.</summary>
public class ClienteCatalogosTests
{
    private static readonly Uri _base = new("http://api.prueba:5080");

    private static string Ejemplo(string archivo) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "contrato", archivo));

    private static HttpResponseMessage Xml(HttpStatusCode estado, string cuerpo) =>
        new(estado) { Content = new StringContent(cuerpo, Encoding.UTF8, "application/xml") };

    private static (ClienteCatalogos Cliente, ManejadorFalso Manejador) Crear(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        var manejador = new ManejadorFalso(responder);
        return (new ClienteCatalogos(new HttpClient(manejador) { BaseAddress = _base }), manejador);
    }

    [Fact]
    public async Task Listar_pide_api_catalogos_con_Accept_xml_y_deserializa()
    {
        var (cliente, manejador) = Crear(_ => Xml(HttpStatusCode.OK, Ejemplo("catalogos.xml")));

        var catalogos = await cliente.ListarAsync();

        var peticion = Assert.Single(manejador.Peticiones);
        Assert.Equal(HttpMethod.Get, peticion.Metodo);
        Assert.Equal("/api/catalogos", peticion.Uri.AbsolutePath);
        Assert.Equal(_base.Host, peticion.Uri.Host);
        Assert.Contains("application/xml", peticion.Accept);
        Assert.Equal(2, catalogos.Catalogos.Count);
        Assert.Equal("Prueba CPU", catalogos.Catalogos[0].Nombre);
        Assert.Equal("MEMORIA", catalogos.Catalogos[1].Criterio);
    }

    [Fact]
    public async Task ObtenerProcesos_pide_la_ruta_del_catalogo_y_deserializa()
    {
        var (cliente, manejador) = Crear(_ => Xml(HttpStatusCode.OK, Ejemplo("catalogo-procesos.xml")));

        var catalogo = await cliente.ObtenerProcesosAsync(1);

        var peticion = Assert.Single(manejador.Peticiones);
        Assert.Equal("/api/catalogos/1/procesos", peticion.Uri.AbsolutePath);
        Assert.Contains("application/xml", peticion.Accept);
        Assert.Equal(3, catalogo.Procesos.Count);
        Assert.Equal(4120, catalogo.Procesos[0].Pid);
        Assert.Equal(1, catalogo.Procesos[1].Prioridad);
    }

    [Fact]
    public async Task ObtenerPlanificacion_envia_th_y_quantum_en_el_query_string()
    {
        var (cliente, manejador) = Crear(_ => Xml(HttpStatusCode.OK, Ejemplo("planificacion.xml")));

        var plan = await cliente.ObtenerPlanificacionAsync(1, 100, 5);

        var peticion = Assert.Single(manejador.Peticiones);
        Assert.Equal("/api/planificacion/1", peticion.Uri.AbsolutePath);
        Assert.Equal("?th=100&quantum=5", peticion.Uri.Query);
        Assert.Contains("application/xml", peticion.Accept);
        Assert.Equal(100, plan.TH);
        Assert.Equal(5, plan.Quantum);
        Assert.Equal(3, plan.Procesos.Count);
        Assert.Equal(3300, plan.Procesos[1].RafagaMs);
    }

    [Fact]
    public async Task Respuesta_404_con_Error_lanza_ErrorServicioException_con_codigo_y_mensaje()
    {
        var (cliente, _) = Crear(_ => Xml(HttpStatusCode.NotFound, Ejemplo("error.xml")));

        var error = await Assert.ThrowsAsync<ErrorServicioException>(() => cliente.ObtenerProcesosAsync(99));

        Assert.Equal(404, error.Codigo);
        Assert.Contains("99", error.Message);
    }

    [Fact]
    public async Task Respuesta_500_sin_cuerpo_usa_el_status_y_un_mensaje_generico()
    {
        var (cliente, _) = Crear(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var error = await Assert.ThrowsAsync<ErrorServicioException>(() => cliente.ListarAsync());

        Assert.Equal(500, error.Codigo);
        Assert.Contains("500", error.Message);
    }

    [Fact]
    public async Task Respuesta_no_2xx_con_cuerpo_que_no_es_Error_usa_el_status()
    {
        var (cliente, _) = Crear(_ => Xml(HttpStatusCode.BadGateway, "<html>Bad gateway</html>"));

        var error = await Assert.ThrowsAsync<ErrorServicioException>(() => cliente.ListarAsync());

        Assert.Equal(502, error.Codigo);
    }

    [Fact]
    public async Task Api_caida_se_informa_como_503_con_la_direccion()
    {
        var (cliente, _) = Crear(_ => throw new HttpRequestException("Connection refused"));

        var error = await Assert.ThrowsAsync<ErrorServicioException>(() => cliente.ObtenerPlanificacionAsync(1, 100, 5));

        Assert.Equal(503, error.Codigo);
        Assert.Equal($"El servicio REST no está disponible en {_base}", error.Message);
    }

    [Fact]
    public async Task Respuesta_200_con_XML_invalido_lanza_ErrorServicioException()
    {
        var (cliente, _) = Crear(_ => Xml(HttpStatusCode.OK, "esto no es xml"));

        var error = await Assert.ThrowsAsync<ErrorServicioException>(() => cliente.ListarAsync());

        Assert.Equal(502, error.Codigo);
    }

    [Fact]
    public async Task Cancelar_propaga_la_cancelacion_y_no_se_confunde_con_Api_caida()
    {
        var (cliente, _) = Crear(_ => Xml(HttpStatusCode.OK, Ejemplo("catalogos.xml")));
        using var fuente = new CancellationTokenSource();
        await fuente.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cliente.ListarAsync(fuente.Token));
    }

    [Fact]
    public async Task Mapeo_convierte_el_catalogo_en_ProcesoEntrada_con_su_prioridad()
    {
        var (cliente, _) = Crear(_ => Xml(HttpStatusCode.OK, Ejemplo("catalogo-procesos.xml")));
        var catalogo = await cliente.ObtenerProcesosAsync(1);

        var entradas = MapeoEntrada.AProcesosEntrada(catalogo);

        Assert.Equal(3, entradas.Count);
        Assert.Equal(4120, entradas[0].Pid);
        Assert.Equal("WINWORD", entradas[0].Nombre);
        Assert.Equal("Microsoft Word", entradas[0].Descripcion);
        Assert.Equal(Prioridad.Expulsivo, entradas[0].Prioridad);
        Assert.Equal(Prioridad.NoExpulsivo, entradas[1].Prioridad);
        Assert.Null(entradas[0].QuantumPropio);
    }

    [Fact]
    public void Mapeo_rechaza_una_prioridad_fuera_del_contrato()
    {
        var catalogo = new Core.Contrato.CatalogoXml { Procesos = [new() { Pid = 7, Prioridad = 2 }] };

        var error = Assert.Throws<InvalidDataException>(() => MapeoEntrada.AProcesosEntrada(catalogo));

        Assert.Contains("7", error.Message);
    }

    [Fact]
    public void Mapeo_de_un_catalogo_vacio_devuelve_lista_vacia()
    {
        Assert.Empty(MapeoEntrada.AProcesosEntrada(new Core.Contrato.CatalogoXml()));
    }

    private sealed record PeticionRegistrada(HttpMethod Metodo, Uri Uri, string Accept);

    private sealed class ManejadorFalso(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        public List<PeticionRegistrada> Peticiones { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage peticion, CancellationToken cancelacion)
        {
            cancelacion.ThrowIfCancellationRequested();
            Peticiones.Add(new PeticionRegistrada(peticion.Method, peticion.RequestUri!, peticion.Headers.Accept.ToString()));
            return Task.FromResult(responder(peticion));
        }
    }
}
