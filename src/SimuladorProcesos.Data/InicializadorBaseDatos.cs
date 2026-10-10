using SimuladorProcesos.Core;

namespace SimuladorProcesos.Data;

/// <summary>Crea la carpeta de datos y el esquema si no existen. Lo llaman la Api y la App 1 al arrancar.</summary>
public static class InicializadorBaseDatos
{
    public static async Task AsegurarCreadaAsync(SimuladorDbContext contexto, CancellationToken cancelacion = default)
    {
        Directory.CreateDirectory(RutasSimulador.CarpetaDatos);
        await contexto.Database.EnsureCreatedAsync(cancelacion);
    }
}
