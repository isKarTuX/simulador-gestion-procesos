using SimuladorProcesos.Core.Modelos;

namespace SimuladorProcesos.Desktop.Servicios.Recolector;

/// <summary>Frontera con el SO: devuelve muestras crudas de los procesos vivos, sin ordenar ni recortar por cantidad.</summary>
public interface ILectorProcesos
{
    /// <summary>Con Memoria llena MemoriaBytes; con Cpu llena CpuInicial y CpuFinal (dos lecturas separadas en el tiempo).</summary>
    Task<IReadOnlyList<MuestraProceso>> LeerAsync(CriterioSeleccion criterio, CancellationToken cancelacion = default);
}
