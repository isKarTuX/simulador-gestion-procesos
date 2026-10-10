using System.Net;
using SimuladorProcesos.Core.Contrato;

namespace SimuladorProcesos.Api.Tests;

/// <summary>GET /api/catalogos y GET /api/catalogos/{id}/procesos (CONTEXT §5.2).</summary>
public class CatalogosEndpointTests
{
    [Fact]
    public async Task Listar_devuelve_un_resumen_por_catalogo_sin_procesos()
    {
        var repositorio = new RepositorioEnMemoria(DatosContrato.CatalogoCpu(), DatosContrato.CatalogoMemoria());

        var respuesta = await FabricaApi.PedirAsync(repositorio, "/api/catalogos");

        Assert.Equal(HttpStatusCode.OK, respuesta.Estado);
        var catalogos = SerializadorXml.Deserializar<CatalogosXml>(respuesta.Cuerpo).Catalogos;
        Assert.Equal(2, catalogos.Count);
        Assert.Collection(
            catalogos,
            cpu =>
            {
                Assert.Equal(1, cpu.Id);
                Assert.Equal(1, cpu.Numero);
                Assert.Equal("Prueba CPU", cpu.Nombre);
                Assert.Equal(new DateTime(2026, 10, 11, 10, 30, 0), cpu.Fecha);
                Assert.Equal("CPU", cpu.Criterio);
                Assert.Equal(3, cpu.Cantidad);
            },
            memoria =>
            {
                Assert.Equal(2, memoria.Id);
                Assert.Equal(7, memoria.Numero);
                Assert.Equal("MEMORIA", memoria.Criterio);
                Assert.Equal(5, memoria.Cantidad);
            });
    }

    [Fact]
    public async Task Listar_sin_catalogos_devuelve_la_raiz_vacia_y_200()
    {
        var respuesta = await FabricaApi.PedirAsync(new RepositorioEnMemoria(), "/api/catalogos");

        Assert.Equal(HttpStatusCode.OK, respuesta.Estado);
        Assert.Empty(SerializadorXml.Deserializar<CatalogosXml>(respuesta.Cuerpo).Catalogos);
    }

    [Fact]
    public async Task Procesos_devuelve_el_catalogo_con_todos_sus_procesos()
    {
        var repositorio = new RepositorioEnMemoria(DatosContrato.CatalogoCpu(), DatosContrato.CatalogoMemoria());

        var respuesta = await FabricaApi.PedirAsync(repositorio, "/api/catalogos/1/procesos");

        Assert.Equal(HttpStatusCode.OK, respuesta.Estado);
        var catalogo = SerializadorXml.Deserializar<CatalogoXml>(respuesta.Cuerpo);
        Assert.Equal(1, catalogo.Id);
        Assert.Equal("CPU", catalogo.Criterio);
        Assert.Equal(3, catalogo.Cantidad);
        Assert.Equal(3, catalogo.Procesos.Count);

        var svchost = catalogo.Procesos[1];
        Assert.Equal(812, svchost.Pid);
        Assert.Equal("svchost", svchost.Nombre);
        Assert.Equal("SYSTEM", svchost.Usuario);
        Assert.Equal("Host Process for Windows Services", svchost.Descripcion);
        Assert.Equal(1, svchost.Prioridad);
        Assert.Equal(0, catalogo.Procesos[0].Prioridad);
    }

    [Fact]
    public async Task Procesos_de_un_catalogo_solo_trae_los_de_ese_catalogo()
    {
        var repositorio = new RepositorioEnMemoria(DatosContrato.CatalogoCpu(), DatosContrato.CatalogoMemoria());

        var respuesta = await FabricaApi.PedirAsync(repositorio, "/api/catalogos/2/procesos");

        var catalogo = SerializadorXml.Deserializar<CatalogoXml>(respuesta.Cuerpo);
        Assert.Equal(2, catalogo.Id);
        Assert.Equal("MEMORIA", catalogo.Criterio);
        Assert.Empty(catalogo.Procesos);
    }

    [Fact]
    public async Task Procesos_de_un_catalogo_inexistente_responde_404_con_Error()
    {
        var repositorio = new RepositorioEnMemoria(DatosContrato.CatalogoCpu());

        var respuesta = await FabricaApi.PedirAsync(repositorio, "/api/catalogos/99/procesos");

        Assert.Equal(HttpStatusCode.NotFound, respuesta.Estado);
        var error = SerializadorXml.Deserializar<ErrorXml>(respuesta.Cuerpo);
        Assert.Equal(404, error.Codigo);
        Assert.Equal("No existe el catálogo con id 99.", error.Mensaje);
    }
}
