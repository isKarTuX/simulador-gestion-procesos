using System.Xml.Linq;

namespace SimuladorProcesos.Api.Tests;

/// <summary>Compara la respuesta con los archivos de docs/contrato: mismos elementos, atributos, orden y valores.</summary>
public class EstructuraContratoTests
{
    [Fact]
    public async Task Catalogos_coincide_con_el_ejemplo_del_contrato()
    {
        var repositorio = new RepositorioEnMemoria(DatosContrato.CatalogoCpu(), DatosContrato.CatalogoMemoria());

        var respuesta = await FabricaApi.PedirAsync(repositorio, "/api/catalogos");

        AssertMismaEstructura("catalogos.xml", respuesta.Cuerpo);
    }

    [Fact]
    public async Task Procesos_de_un_catalogo_coincide_con_el_ejemplo_del_contrato()
    {
        var repositorio = new RepositorioEnMemoria(DatosContrato.CatalogoCpu());

        var respuesta = await FabricaApi.PedirAsync(repositorio, "/api/catalogos/1/procesos");

        AssertMismaEstructura("catalogo-procesos.xml", respuesta.Cuerpo);
    }

    [Fact]
    public async Task Planificacion_coincide_con_el_ejemplo_del_contrato()
    {
        var repositorio = new RepositorioEnMemoria(DatosContrato.CatalogoCpu());

        var respuesta = await FabricaApi.PedirAsync(repositorio, "/api/planificacion/1?th=100&quantum=5");

        AssertMismaEstructura("planificacion.xml", respuesta.Cuerpo);
    }

    [Fact]
    public async Task Error_coincide_con_el_ejemplo_del_contrato()
    {
        var respuesta = await FabricaApi.PedirAsync(new RepositorioEnMemoria(), "/api/catalogos/99/procesos");

        AssertMismaEstructura("error.xml", respuesta.Cuerpo);
    }

    private static void AssertMismaEstructura(string ejemplo, string respuesta)
    {
        var esperado = Normalizar(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "contrato", ejemplo)));
        var obtenido = Normalizar(respuesta);

        // Si falla, ToString() muestra ambos árboles completos en el mensaje.
        Assert.True(
            XNode.DeepEquals(esperado, obtenido),
            $"La respuesta no coincide con docs/contrato/{ejemplo}.\n--- esperado ---\n{esperado}\n--- obtenido ---\n{obtenido}");
    }

    // Sin comentarios ni declaraciones xmlns: lo único que importa es el árbol de elementos y atributos.
    private static XElement Normalizar(string xml)
    {
        var raiz = XDocument.Parse(xml).Root!;
        raiz.DescendantNodes().OfType<XComment>().Remove();
        foreach (var elemento in raiz.DescendantsAndSelf())
        {
            elemento.Attributes().Where(a => a.IsNamespaceDeclaration).Remove();
        }

        return raiz;
    }
}
