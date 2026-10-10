using SimuladorProcesos.Core;
using SimuladorProcesos.Core.Contrato;
using SimuladorProcesos.Core.Modelos;

namespace SimuladorProcesos.Api.Mapeo;

/// <summary>Convierte el modelo de dominio a los DTO congelados de Core/Contrato (CONTEXT §5.3).</summary>
public static class MapeoContrato
{
    public static CatalogoResumenXml ACatalogoResumenXml(this Catalogo catalogo) => new()
    {
        Id = catalogo.Id,
        Numero = catalogo.Numero,
        Nombre = catalogo.Nombre,
        Fecha = catalogo.Fecha,
        Criterio = TextoCriterio(catalogo.Criterio),
        Cantidad = catalogo.Cantidad,
    };

    public static CatalogoXml ACatalogoXml(this Catalogo catalogo) => new()
    {
        Id = catalogo.Id,
        Numero = catalogo.Numero,
        Nombre = catalogo.Nombre,
        Fecha = catalogo.Fecha,
        Criterio = TextoCriterio(catalogo.Criterio),
        Cantidad = catalogo.Cantidad,
        Procesos = [.. catalogo.Procesos.Select(p => new ProcesoXml
        {
            Pid = p.Pid,
            Nombre = p.Nombre,
            Usuario = p.Usuario,
            Descripcion = p.Descripcion,
            Prioridad = (int)p.Prioridad,
        })],
    };

    /// <summary>La ráfaga se calcula aquí con TH y se entrega ya resuelta: la App 2 no repite la fórmula.</summary>
    public static PlanificacionXml APlanificacionXml(this Catalogo catalogo, int th, int quantum) => new()
    {
        CatalogoId = catalogo.Id,
        TH = th,
        Quantum = quantum,
        Procesos = [.. catalogo.Procesos.Select(p => new ProcesoPlanificacionXml
        {
            Pid = p.Pid,
            Nombre = p.Nombre,
            Caracteres = p.Descripcion.Length,
            RafagaMs = Formulas.Rafaga(th, p.Descripcion.Length),
            Prioridad = (int)p.Prioridad,
        })],
    };

    // El texto del contrato es "CPU" | "MEMORIA", distinto del nombre del enum.
    private static string TextoCriterio(CriterioSeleccion criterio) => criterio switch
    {
        CriterioSeleccion.Cpu => "CPU",
        CriterioSeleccion.Memoria => "MEMORIA",
        _ => throw new ArgumentOutOfRangeException(nameof(criterio), criterio, "Criterio desconocido."),
    };
}
