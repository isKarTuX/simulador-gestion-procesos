using System.Net;
using SimuladorProcesos.Core.Contrato;

namespace SimuladorProcesos.Api.Tests;

/// <summary>GET /api/planificacion/{id}?th=&amp;quantum= (CONTEXT §5.2).</summary>
public class PlanificacionEndpointTests
{
    private static RepositorioEnMemoria Repositorio() => new(DatosContrato.CatalogoCpu());

    [Fact]
    public async Task Calcula_la_rafaga_como_TH_por_caracteres_de_la_descripcion()
    {
        var respuesta = await FabricaApi.PedirAsync(Repositorio(), "/api/planificacion/1?th=100&quantum=5");

        Assert.Equal(HttpStatusCode.OK, respuesta.Estado);
        var plan = SerializadorXml.Deserializar<PlanificacionXml>(respuesta.Cuerpo);
        Assert.Equal(1, plan.CatalogoId);
        Assert.Equal("RoundRobin", plan.Algoritmo);
        Assert.Equal(100, plan.TH);
        Assert.Equal(5, plan.Quantum);
        Assert.Collection(
            plan.Procesos,
            word =>
            {
                Assert.Equal(4120, word.Pid);
                Assert.Equal("WINWORD", word.Nombre);
                Assert.Equal(14, word.Caracteres);
                Assert.Equal(1400, word.RafagaMs);
                Assert.Equal(0, word.Prioridad);
            },
            svchost =>
            {
                Assert.Equal(33, svchost.Caracteres);
                Assert.Equal(3300, svchost.RafagaMs);
                Assert.Equal(1, svchost.Prioridad);
            },
            chrome => Assert.Equal(1300, chrome.RafagaMs));
    }

    [Fact]
    public async Task Sin_parametros_usa_TH_100_y_quantum_5()
    {
        var respuesta = await FabricaApi.PedirAsync(Repositorio(), "/api/planificacion/1");

        var plan = SerializadorXml.Deserializar<PlanificacionXml>(respuesta.Cuerpo);
        Assert.Equal(100, plan.TH);
        Assert.Equal(5, plan.Quantum);
        Assert.Equal(1400, plan.Procesos[0].RafagaMs);
    }

    [Fact]
    public async Task Usa_el_TH_y_el_quantum_pedidos()
    {
        var respuesta = await FabricaApi.PedirAsync(Repositorio(), "/api/planificacion/1?th=20&quantum=3");

        var plan = SerializadorXml.Deserializar<PlanificacionXml>(respuesta.Cuerpo);
        Assert.Equal(20, plan.TH);
        Assert.Equal(3, plan.Quantum);
        Assert.Equal(280, plan.Procesos[0].RafagaMs);
    }

    [Fact]
    public async Task La_rafaga_no_se_desborda_con_un_TH_enorme()
    {
        var respuesta = await FabricaApi.PedirAsync(Repositorio(), $"/api/planificacion/1?th={int.MaxValue}");

        var plan = SerializadorXml.Deserializar<PlanificacionXml>(respuesta.Cuerpo);
        Assert.Equal((long)int.MaxValue * 33, plan.Procesos[1].RafagaMs);
    }

    [Fact]
    public async Task Catalogo_inexistente_responde_404_con_Error()
    {
        var respuesta = await FabricaApi.PedirAsync(Repositorio(), "/api/planificacion/99");

        Assert.Equal(HttpStatusCode.NotFound, respuesta.Estado);
        var error = SerializadorXml.Deserializar<ErrorXml>(respuesta.Cuerpo);
        Assert.Equal(404, error.Codigo);
        Assert.Equal("No existe el catálogo con id 99.", error.Mensaje);
    }

    [Theory]
    [InlineData("th=0")]
    [InlineData("th=-5")]
    [InlineData("quantum=0")]
    [InlineData("quantum=-1")]
    [InlineData("th=0&quantum=0")]
    public async Task TH_o_quantum_no_positivos_responden_400_con_Error(string consulta)
    {
        var respuesta = await FabricaApi.PedirAsync(Repositorio(), $"/api/planificacion/1?{consulta}");

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.Estado);
        Assert.Equal("application/xml", respuesta.TipoContenido);
        Assert.Equal(400, SerializadorXml.Deserializar<ErrorXml>(respuesta.Cuerpo).Codigo);
    }

    [Theory]
    [InlineData("th=abc")]
    [InlineData("quantum=1.5")]
    [InlineData("th=99999999999")]
    public async Task TH_o_quantum_que_no_son_enteros_responden_400_con_Error(string consulta)
    {
        var respuesta = await FabricaApi.PedirAsync(Repositorio(), $"/api/planificacion/1?{consulta}");

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.Estado);
        var error = SerializadorXml.Deserializar<ErrorXml>(respuesta.Cuerpo);
        Assert.Equal(400, error.Codigo);
        Assert.NotEmpty(error.Mensaje);
    }

    [Fact]
    public async Task Un_parametro_invalido_responde_400_antes_que_404()
    {
        var respuesta = await FabricaApi.PedirAsync(Repositorio(), "/api/planificacion/99?th=0");

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.Estado);
    }
}
