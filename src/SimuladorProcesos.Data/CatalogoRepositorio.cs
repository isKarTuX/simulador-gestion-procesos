using Microsoft.EntityFrameworkCore;
using SimuladorProcesos.Core.Modelos;

namespace SimuladorProcesos.Data;

/// <summary>Implementación EF Core + SQLite de ICatalogoRepositorio.</summary>
public sealed class CatalogoRepositorio(SimuladorDbContext contexto) : ICatalogoRepositorio
{
    public async Task<IReadOnlyList<Catalogo>> ListarAsync(CancellationToken cancelacion = default) =>
        // Sin Include: el listado no necesita los procesos y así la consulta es barata.
        await contexto.Catalogos
            .AsNoTracking()
            .OrderBy(c => c.Numero)
            .ToListAsync(cancelacion);

    public async Task<Catalogo?> ObtenerConProcesosAsync(int id, CancellationToken cancelacion = default)
    {
        var catalogo = await contexto.Catalogos
            .AsNoTracking()
            .Include(c => c.Procesos)
            .FirstOrDefaultAsync(c => c.Id == id, cancelacion);

        // El Id autoincremental refleja el orden de captura; EF no garantiza el orden del Include.
        catalogo?.Procesos.Sort((a, b) => a.Id.CompareTo(b.Id));
        return catalogo;
    }

    public async Task<Catalogo> GuardarAsync(Catalogo catalogo, CancellationToken cancelacion = default)
    {
        Validar(catalogo);

        if (await contexto.Catalogos.AnyAsync(c => c.Numero == catalogo.Numero, cancelacion))
        {
            throw NumeroRepetido(catalogo.Numero);
        }

        // Fecha en default significa "ahora"; el llamador no tiene que rellenarla.
        if (catalogo.Fecha == default)
        {
            catalogo.Fecha = DateTime.Now;
        }

        contexto.Catalogos.Add(catalogo);
        try
        {
            await contexto.SaveChangesAsync(cancelacion);
        }
        catch (DbUpdateException ex)
        {
            // Deja el contexto limpio para que el llamador pueda reutilizarlo tras el error.
            contexto.ChangeTracker.Clear();

            // Otra instancia (la App 1 u otra petición) pudo ganar la carrera entre la comprobación y el guardado.
            if (await contexto.Catalogos.AnyAsync(c => c.Numero == catalogo.Numero, cancelacion))
            {
                throw NumeroRepetido(catalogo.Numero, ex);
            }

            throw;
        }

        return catalogo;
    }

    public async Task<int> SiguienteNumeroAsync(CancellationToken cancelacion = default)
    {
        // Máximo anulable: con la tabla vacía Max devuelve null y el siguiente es 1.
        var maximo = await contexto.Catalogos.MaxAsync(c => (int?)c.Numero, cancelacion);
        return (maximo ?? 0) + 1;
    }

    private static void Validar(Catalogo catalogo)
    {
        ArgumentNullException.ThrowIfNull(catalogo);

        if (string.IsNullOrWhiteSpace(catalogo.Nombre))
        {
            throw new ArgumentException("El nombre del catálogo no puede estar vacío.", nameof(catalogo));
        }

        if (catalogo.Cantidad <= 0)
        {
            throw new ArgumentException("La cantidad de procesos debe ser mayor que cero.", nameof(catalogo));
        }

        if (catalogo.Numero <= 0)
        {
            throw new ArgumentException("El número de catálogo debe ser mayor que cero.", nameof(catalogo));
        }
    }

    private static InvalidOperationException NumeroRepetido(int numero, Exception? causa = null) =>
        new($"Ya existe un catálogo con el número {numero}. Elige otro número o usa el siguiente disponible.", causa);
}
