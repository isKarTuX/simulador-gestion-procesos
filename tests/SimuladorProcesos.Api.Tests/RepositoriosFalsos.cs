using SimuladorProcesos.Core.Modelos;
using SimuladorProcesos.Data;

namespace SimuladorProcesos.Api.Tests;

/// <summary>Repositorio en memoria: la Api se prueba sin depender de la implementación EF (subtarea 1.1).</summary>
public sealed class RepositorioEnMemoria(params Catalogo[] catalogos) : ICatalogoRepositorio
{
    private readonly List<Catalogo> _catalogos = [.. catalogos];

    public Task<IReadOnlyList<Catalogo>> ListarAsync(CancellationToken cancelacion = default) =>
        Task.FromResult<IReadOnlyList<Catalogo>>([.. _catalogos]);

    public Task<Catalogo?> ObtenerConProcesosAsync(int id, CancellationToken cancelacion = default) =>
        Task.FromResult(_catalogos.FirstOrDefault(c => c.Id == id));

    public Task<Catalogo> GuardarAsync(Catalogo catalogo, CancellationToken cancelacion = default)
    {
        _catalogos.Add(catalogo);
        return Task.FromResult(catalogo);
    }

    public Task<int> SiguienteNumeroAsync(CancellationToken cancelacion = default) =>
        Task.FromResult(_catalogos.Count == 0 ? 1 : _catalogos.Max(c => c.Numero) + 1);
}

/// <summary>Simula una BD caída (p. ej. bloqueada por la App 1) para probar el 500 en XML.</summary>
public sealed class RepositorioQueFalla : ICatalogoRepositorio
{
    public Task<IReadOnlyList<Catalogo>> ListarAsync(CancellationToken cancelacion = default) => throw Falla();

    public Task<Catalogo?> ObtenerConProcesosAsync(int id, CancellationToken cancelacion = default) => throw Falla();

    public Task<Catalogo> GuardarAsync(Catalogo catalogo, CancellationToken cancelacion = default) => throw Falla();

    public Task<int> SiguienteNumeroAsync(CancellationToken cancelacion = default) => throw Falla();

    private static InvalidOperationException Falla() => new("La base de datos no responde.");
}
