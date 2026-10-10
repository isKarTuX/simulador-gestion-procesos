using Microsoft.AspNetCore.Mvc.Testing;

namespace SimuladorProcesos.Api.Tests;

/// <summary>Humo del contrato: toda ruta responde application/xml. El agente backend agrega las pruebas por endpoint.</summary>
public class RespuestaXmlTests(WebApplicationFactory<Program> fabrica) : IClassFixture<WebApplicationFactory<Program>>
{
    [Theory]
    [InlineData("/api/catalogos")]
    [InlineData("/api/catalogos/1/procesos")]
    [InlineData("/api/planificacion/1")]
    public async Task Las_rutas_del_contrato_responden_en_XML(string ruta)
    {
        var cliente = fabrica.CreateClient();

        var respuesta = await cliente.GetAsync(ruta);

        Assert.Equal("application/xml", respuesta.Content.Headers.ContentType?.MediaType);
    }
}
