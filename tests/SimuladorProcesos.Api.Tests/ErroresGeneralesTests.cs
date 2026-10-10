using System.Net;
using SimuladorProcesos.Core.Contrato;

namespace SimuladorProcesos.Api.Tests;

/// <summary>Errores que no vienen de un controlador también salen como &lt;Error&gt; en XML.</summary>
public class ErroresGeneralesTests
{
    [Theory]
    [InlineData("/api/no-existe")]
    [InlineData("/api/catalogos/abc/procesos")]
    public async Task Una_ruta_inexistente_responde_404_con_Error(string ruta)
    {
        var respuesta = await FabricaApi.PedirAsync(new RepositorioEnMemoria(), ruta);

        Assert.Equal(HttpStatusCode.NotFound, respuesta.Estado);
        Assert.Equal("application/xml", respuesta.TipoContenido);
        Assert.Equal(404, SerializadorXml.Deserializar<ErrorXml>(respuesta.Cuerpo).Codigo);
    }

    [Fact]
    public async Task Un_metodo_no_permitido_responde_405_con_Error()
    {
        var respuesta = await FabricaApi.PedirAsync(new RepositorioEnMemoria(), "/api/catalogos", metodo: HttpMethod.Post);

        Assert.Equal(HttpStatusCode.MethodNotAllowed, respuesta.Estado);
        Assert.Equal(405, SerializadorXml.Deserializar<ErrorXml>(respuesta.Cuerpo).Codigo);
    }

    [Theory]
    [InlineData("/api/catalogos")]
    [InlineData("/api/catalogos/1/procesos")]
    [InlineData("/api/planificacion/1")]
    public async Task Si_la_BD_falla_responde_500_con_Error_sin_filtrar_el_detalle(string ruta)
    {
        var respuesta = await FabricaApi.PedirAsync(new RepositorioQueFalla(), ruta);

        Assert.Equal(HttpStatusCode.InternalServerError, respuesta.Estado);
        Assert.Equal("application/xml", respuesta.TipoContenido);
        var error = SerializadorXml.Deserializar<ErrorXml>(respuesta.Cuerpo);
        Assert.Equal(500, error.Codigo);
        Assert.DoesNotContain("base de datos no responde", error.Mensaje);
    }
}
