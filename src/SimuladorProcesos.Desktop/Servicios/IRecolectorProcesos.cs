using SimuladorProcesos.Core.Modelos;

namespace SimuladorProcesos.Desktop.Servicios;

/// <summary>M1: captura procesos reales del SO. Implementa: subtarea 1.3 en Servicios/Recolector.</summary>
public interface IRecolectorProcesos
{
    /// <summary>
    /// Devuelve los <paramref name="cantidad"/> procesos con mayor uso del criterio, ya con usuario, descripción y prioridad.
    /// Omite los que no se pueden leer y sigue hasta completar la cantidad (o hasta agotar los procesos del SO).
    /// </summary>
    Task<IReadOnlyList<ProcesoCatalogo>> CapturarAsync(int cantidad, CriterioSeleccion criterio, CancellationToken cancelacion = default);
}
