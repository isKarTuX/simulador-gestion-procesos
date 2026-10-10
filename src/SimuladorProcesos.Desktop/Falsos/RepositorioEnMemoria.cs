using SimuladorProcesos.Core.Modelos;
using SimuladorProcesos.Data;

namespace SimuladorProcesos.Desktop.Falsos;

/// <summary>Repositorio que guarda los catálogos en una lista; cumple el mismo contrato que el de EF Core, incluido el error por número repetido.</summary>
public sealed class RepositorioEnMemoria : ICatalogoRepositorio
{
    private readonly List<Catalogo> _catalogos = [];
    private readonly Lock _candado = new();
    private int _ultimoId;

    public Task<IReadOnlyList<Catalogo>> ListarAsync(CancellationToken cancelacion = default)
    {
        lock (_candado)
        {
            IReadOnlyList<Catalogo> copia = [.. _catalogos.OrderBy(c => c.Numero)];
            return Task.FromResult(copia);
        }
    }

    public Task<Catalogo?> ObtenerConProcesosAsync(int id, CancellationToken cancelacion = default)
    {
        lock (_candado)
        {
            return Task.FromResult(_catalogos.FirstOrDefault(c => c.Id == id));
        }
    }

    public Task<Catalogo> GuardarAsync(Catalogo catalogo, CancellationToken cancelacion = default)
    {
        lock (_candado)
        {
            if (_catalogos.Any(c => c.Numero == catalogo.Numero))
            {
                throw new InvalidOperationException($"Ya existe un catálogo con el número {catalogo.Numero}.");
            }

            catalogo.Id = ++_ultimoId;
            foreach (var proceso in catalogo.Procesos)
            {
                proceso.CatalogoId = catalogo.Id;
            }

            _catalogos.Add(catalogo);
            return Task.FromResult(catalogo);
        }
    }

    public Task<int> SiguienteNumeroAsync(CancellationToken cancelacion = default)
    {
        lock (_candado)
        {
            return Task.FromResult(_catalogos.Count == 0 ? 1 : _catalogos.Max(c => c.Numero) + 1);
        }
    }
}
