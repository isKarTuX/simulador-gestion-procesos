using SimuladorProcesos.Core.Modelos;

namespace SimuladorProcesos.Api.Tests;

/// <summary>Los mismos datos que los ejemplos de docs/contrato, para comparar la respuesta con el archivo.</summary>
public static class DatosContrato
{
    public static Catalogo CatalogoCpu() => new()
    {
        Id = 1,
        Numero = 1,
        Nombre = "Prueba CPU",
        Fecha = new DateTime(2026, 10, 11, 10, 30, 0),
        Criterio = CriterioSeleccion.Cpu,
        Cantidad = 3,
        Procesos =
        [
            Proceso(1, 4120, "WINWORD", "juan", "Microsoft Word", Prioridad.Expulsivo),
            Proceso(1, 812, "svchost", "SYSTEM", "Host Process for Windows Services", Prioridad.NoExpulsivo),
            Proceso(1, 9932, "chrome", "juan", "Google Chrome", Prioridad.Expulsivo),
        ],
    };

    public static Catalogo CatalogoMemoria() => new()
    {
        Id = 2,
        Numero = 7,
        Nombre = "Prueba memoria",
        Fecha = new DateTime(2026, 10, 11, 11, 0, 0),
        Criterio = CriterioSeleccion.Memoria,
        Cantidad = 5,
    };

    private static ProcesoCatalogo Proceso(int catalogoId, int pid, string nombre, string usuario, string descripcion, Prioridad prioridad) => new()
    {
        CatalogoId = catalogoId,
        Pid = pid,
        Nombre = nombre,
        Usuario = usuario,
        Descripcion = descripcion,
        Prioridad = prioridad,
    };
}
