using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SimuladorProcesos.Core;

namespace SimuladorProcesos.Data;

/// <summary>Registro único de la capa de datos para la App 1 y la Api.</summary>
public static class ExtensionesServicios
{
    public static IServiceCollection AddSimuladorData(this IServiceCollection servicios, string? cadenaConexion = null)
    {
        var cadena = string.IsNullOrWhiteSpace(cadenaConexion) ? RutasSimulador.CadenaConexionPorDefecto : cadenaConexion;
        servicios.AddDbContext<SimuladorDbContext>(opciones => opciones.UseSqlite(cadena));
        servicios.AddScoped<ICatalogoRepositorio, CatalogoRepositorio>();
        return servicios;
    }
}
