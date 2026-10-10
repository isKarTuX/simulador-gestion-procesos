using SimuladorProcesos.Web.Servicios;

namespace SimuladorProcesos.Web.Tests;

/// <summary>La subtarea 1.12 agrega aquí las pruebas de ClienteCatalogos con un HttpMessageHandler falso.</summary>
public class ErrorServicioTests
{
    [Fact]
    public void Conserva_el_codigo_y_el_mensaje_de_la_Api()
    {
        var error = new ErrorServicioException(404, "No existe el catálogo con id 99.");

        Assert.Equal(404, error.Codigo);
        Assert.Contains("99", error.Message);
    }
}
