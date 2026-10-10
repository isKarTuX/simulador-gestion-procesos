using SimuladorProcesos.Core.Contrato;

namespace SimuladorProcesos.Core.Tests;

/// <summary>Garantiza que los DTO de Core leen exactamente los ejemplos de docs/contrato.</summary>
public class ContratoXmlTests
{
    private static string Ejemplo(string archivo) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "contrato", archivo));

    [Fact]
    public void Lee_la_lista_de_catalogos()
    {
        var catalogos = SerializadorXml.Deserializar<CatalogosXml>(Ejemplo("catalogos.xml"));

        Assert.Equal(2, catalogos.Catalogos.Count);
        Assert.Equal("MEMORIA", catalogos.Catalogos[1].Criterio);
        Assert.Equal(5, catalogos.Catalogos[1].Cantidad);
    }

    [Fact]
    public void Lee_un_catalogo_con_sus_procesos()
    {
        var catalogo = SerializadorXml.Deserializar<CatalogoXml>(Ejemplo("catalogo-procesos.xml"));

        Assert.Equal(1, catalogo.Numero);
        Assert.Equal(3, catalogo.Procesos.Count);
        Assert.Equal("Microsoft Word", catalogo.Procesos[0].Descripcion);
        Assert.Equal(1, catalogo.Procesos[1].Prioridad);
    }

    [Fact]
    public void Lee_la_planificacion_y_su_rafaga_es_coherente()
    {
        var plan = SerializadorXml.Deserializar<PlanificacionXml>(Ejemplo("planificacion.xml"));

        Assert.Equal("RoundRobin", plan.Algoritmo);
        Assert.All(plan.Procesos, p => Assert.Equal(Formulas.Rafaga(plan.TH, p.Caracteres), p.RafagaMs));
    }

    [Fact]
    public void Lee_el_error()
    {
        var error = SerializadorXml.Deserializar<ErrorXml>(Ejemplo("error.xml"));

        Assert.Equal(404, error.Codigo);
        Assert.NotEmpty(error.Mensaje);
    }

    [Fact]
    public void Serializar_y_deserializar_conserva_los_datos()
    {
        var original = new CatalogoXml
        {
            Id = 3,
            Numero = 9,
            Nombre = "Ida y vuelta",
            Criterio = "CPU",
            Procesos = [new ProcesoXml { Pid = 1, Nombre = "a", Usuario = "u", Descripcion = "ñandú", Prioridad = 0 }],
        };

        var copia = SerializadorXml.Deserializar<CatalogoXml>(SerializadorXml.Serializar(original));

        Assert.Equal(original.Nombre, copia.Nombre);
        Assert.Equal("ñandú", Assert.Single(copia.Procesos).Descripcion);
    }
}
