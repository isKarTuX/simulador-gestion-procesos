using SimuladorProcesos.Core;
using SimuladorProcesos.Core.Modelos;
using SimuladorProcesos.Engine.Modelo;

namespace SimuladorProcesos.Engine.Calculos;

/// <summary>Convierte procesos terminados en las filas de los reportes (T.L, R, P.R, T.R, T.F).</summary>
internal static class CalculadoraTiempos
{
    public static FilaReporte Fila(ProcesoSimulado p)
    {
        ArgumentNullException.ThrowIfNull(p);

        if (p.Estado != EstadoProceso.Terminado || p.TiempoFinalizacionReal is not long tfReal)
        {
            throw new InvalidOperationException($"El proceso '{p.Nombre}' aún no terminó; no se puede calcular su fila de reporte.");
        }

        // T.R usa el TF real (no el del enunciado) porque es el que refleja la espera (CONTEXT §3, duda 4).
        long turnaround = Formulas.Turnaround(tfReal, p.TiempoLlegada);

        return new FilaReporte(
            Proceso: p.Nombre,
            TiempoLlegada: p.TiempoLlegada,
            Rafaga: p.Rafaga,
            PrioridadAsignada: (int)p.Prioridad,
            Turnaround: turnaround,
            TiempoFinalizacion: Formulas.TiempoFinalizacionEnunciado(p.Quantum, p.Ejecuciones),
            TiempoFinalizacionReal: tfReal,
            Espera: Formulas.Espera(turnaround, p.Rafaga));
    }

    /// <summary>Una fila por proceso, conservando el orden de terminación recibido.</summary>
    public static IReadOnlyList<FilaReporte> Reporte(IEnumerable<ProcesoSimulado> terminadosEnOrden)
    {
        ArgumentNullException.ThrowIfNull(terminadosEnOrden);
        return terminadosEnOrden.Select(Fila).ToArray();
    }

    /// <summary>Segundo reporte del enunciado: solo los procesos de prioridad no expulsiva.</summary>
    public static IReadOnlyList<FilaReporte> SoloNoExpulsivos(IReadOnlyList<FilaReporte> filas)
    {
        ArgumentNullException.ThrowIfNull(filas);
        return filas.Where(f => f.PrioridadAsignada == (int)Prioridad.NoExpulsivo).ToArray();
    }
}
