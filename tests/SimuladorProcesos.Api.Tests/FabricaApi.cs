using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SimuladorProcesos.Data;

namespace SimuladorProcesos.Api.Tests;

/// <summary>Respuesta HTTP ya leída: las pruebas solo miran estado, tipo y cuerpo.</summary>
public sealed record RespuestaApi(HttpStatusCode Estado, string? TipoContenido, string Cuerpo);

/// <summary>Api real con un repositorio falso en memoria y sin crear la BD de %LOCALAPPDATA%.</summary>
public sealed class FabricaApi(ICatalogoRepositorio repositorio) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Evita InicializadorBaseDatos: ni la carpeta ni el archivo de la BD real se tocan.
        builder.UseSetting("Simulador:CrearBaseDatosAlArrancar", "false");
        builder.ConfigureTestServices(servicios =>
        {
            servicios.RemoveAll<ICatalogoRepositorio>();
            servicios.AddSingleton(repositorio);
        });
    }

    /// <summary>Levanta la Api con ese repositorio, hace una petición y la cierra.</summary>
    public static async Task<RespuestaApi> PedirAsync(
        ICatalogoRepositorio repositorio, string ruta, string? accept = null, HttpMethod? metodo = null)
    {
        using var fabrica = new FabricaApi(repositorio);
        using var cliente = fabrica.CreateClient();
        using var peticion = new HttpRequestMessage(metodo ?? HttpMethod.Get, ruta);
        if (accept is not null)
        {
            peticion.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse(accept));
        }

        using var respuesta = await cliente.SendAsync(peticion);
        return new RespuestaApi(
            respuesta.StatusCode,
            respuesta.Content.Headers.ContentType?.MediaType,
            await respuesta.Content.ReadAsStringAsync());
    }
}
