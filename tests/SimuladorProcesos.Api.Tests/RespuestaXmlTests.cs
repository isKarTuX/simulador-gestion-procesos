using System.Net;

namespace SimuladorProcesos.Api.Tests;

/// <summary>Toda respuesta, de éxito o de error, es application/xml sin importar el Accept del cliente.</summary>
public class RespuestaXmlTests
{
    private static RepositorioEnMemoria Repositorio() => new(DatosContrato.CatalogoCpu());

    [Theory]
    [InlineData("/api/catalogos")]
    [InlineData("/api/catalogos/1/procesos")]
    [InlineData("/api/planificacion/1")]
    [InlineData("/api/catalogos/99/procesos")]
    [InlineData("/api/planificacion/1?th=0")]
    public async Task Las_rutas_del_contrato_responden_en_XML_sin_Accept(string ruta)
    {
        var respuesta = await FabricaApi.PedirAsync(Repositorio(), ruta);

        Assert.Equal("application/xml", respuesta.TipoContenido);
    }

    [Theory]
    [InlineData("application/json")]
    [InlineData("text/html")]
    public async Task Responde_XML_aunque_el_cliente_pida_otro_formato(string accept)
    {
        var respuesta = await FabricaApi.PedirAsync(Repositorio(), "/api/catalogos", accept);

        Assert.Equal(HttpStatusCode.OK, respuesta.Estado);
        Assert.Equal("application/xml", respuesta.TipoContenido);
    }

    [Theory]
    [InlineData("/api/catalogos")]
    [InlineData("/api/catalogos/1/procesos")]
    [InlineData("/api/planificacion/1")]
    [InlineData("/api/catalogos/99/procesos")]
    public async Task El_XML_lleva_declaracion_y_no_trae_namespaces_xsi_ni_xsd(string ruta)
    {
        var respuesta = await FabricaApi.PedirAsync(Repositorio(), ruta);

        Assert.StartsWith("<?xml version=\"1.0\" encoding=\"utf-8\"?>", respuesta.Cuerpo);
        Assert.DoesNotContain("xmlns", respuesta.Cuerpo);
    }
}
