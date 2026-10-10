using SimuladorProcesos.Core.Modelos;

namespace SimuladorProcesos.Data;

/// <summary>Implementación EF Core de ICatalogoRepositorio. Pendiente: subtarea 1.1 (docs/PROMPTS.md).</summary>
public sealed class CatalogoRepositorio : ICatalogoRepositorio
{
    public Task<IReadOnlyList<Catalogo>> ListarAsync(CancellationToken cancelacion = default) =>
        throw new NotImplementedException("Subtarea 1.1");

    public Task<Catalogo?> ObtenerConProcesosAsync(int id, CancellationToken cancelacion = default) =>
        throw new NotImplementedException("Subtarea 1.1");

    public Task<Catalogo> GuardarAsync(Catalogo catalogo, CancellationToken cancelacion = default) =>
        throw new NotImplementedException("Subtarea 1.1");

    public Task<int> SiguienteNumeroAsync(CancellationToken cancelacion = default) =>
        throw new NotImplementedException("Subtarea 1.1");
}
