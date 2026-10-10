using SimuladorProcesos.Core.Modelos;

namespace SimuladorProcesos.Desktop.Servicios.Recolector;

/// <summary>Ordena las muestras según el criterio; es lógica pura para poder probarla sin tocar el SO.</summary>
public static class SelectorProcesos
{
    /// <summary>
    /// Memoria: WorkingSet64 descendente. CPU: (CpuFinal - CpuInicial) descendente.
    /// Descarta las muestras sin dato para el criterio y desempata por Pid ascendente.
    /// </summary>
    public static IReadOnlyList<MuestraProceso> Ordenar(IEnumerable<MuestraProceso> muestras, CriterioSeleccion criterio)
    {
        ArgumentNullException.ThrowIfNull(muestras);

        return criterio switch
        {
            CriterioSeleccion.Memoria => muestras
                .Where(m => m.MemoriaBytes.HasValue)
                .OrderByDescending(m => m.MemoriaBytes!.Value)
                .ThenBy(m => m.Pid)
                .ToList(),
            CriterioSeleccion.Cpu => muestras
                .Where(m => m.CpuInicial.HasValue && m.CpuFinal.HasValue)
                .OrderByDescending(m => m.CpuFinal!.Value - m.CpuInicial!.Value)
                .ThenBy(m => m.Pid)
                .ToList(),
            _ => throw new ArgumentOutOfRangeException(nameof(criterio), criterio, "Criterio de selección desconocido."),
        };
    }
}
