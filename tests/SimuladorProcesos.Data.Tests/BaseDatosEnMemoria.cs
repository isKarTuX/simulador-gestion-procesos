using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace SimuladorProcesos.Data.Tests;

/// <summary>SQLite en memoria con el esquema real; vive mientras la conexión esté abierta.</summary>
public sealed class BaseDatosEnMemoria : IDisposable
{
    private readonly SqliteConnection _conexion = new("Data Source=:memory:");

    public BaseDatosEnMemoria()
    {
        _conexion.Open();
        using var contexto = CrearContexto();
        contexto.Database.EnsureCreated();
    }

    public SimuladorDbContext CrearContexto() =>
        new(new DbContextOptionsBuilder<SimuladorDbContext>().UseSqlite(_conexion).Options);

    public void Dispose() => _conexion.Dispose();
}
