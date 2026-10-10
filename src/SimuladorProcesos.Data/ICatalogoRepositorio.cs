using SimuladorProcesos.Core.Modelos;

namespace SimuladorProcesos.Data;

/// <summary>Acceso a catálogos compartido por la App 1 (escribe) y la Api (lee). Implementa: agente backend.</summary>
public interface ICatalogoRepositorio
{
    Task<IReadOnlyList<Catalogo>> ListarAsync(CancellationToken cancelacion = default);

    /// <summary>Catálogo con sus procesos, o null si no existe.</summary>
    Task<Catalogo?> ObtenerConProcesosAsync(int id, CancellationToken cancelacion = default);

    /// <summary>Guarda el catálogo y sus procesos. Lanza InvalidOperationException si el número ya existe.</summary>
    Task<Catalogo> GuardarAsync(Catalogo catalogo, CancellationToken cancelacion = default);

    /// <summary>Siguiente número consecutivo sugerido (máximo actual + 1, o 1).</summary>
    Task<int> SiguienteNumeroAsync(CancellationToken cancelacion = default);
}
