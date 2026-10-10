using SimuladorProcesos.Api.Errores;
using SimuladorProcesos.Api.Formato;
using SimuladorProcesos.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSimuladorData(builder.Configuration.GetConnectionString("Simulador"));

// Servicio solo de lectura: basta el formateador de salida XML (sin xmlns:xsi/xsd) y cada controlador declara [Produces("application/xml")].
builder.Services
    .AddControllers(opciones => opciones.OutputFormatters.Add(new FormateadorXmlSinNamespaces()))
    .ConfigureApiBehaviorOptions(opciones =>
    {
        // Un th o quantum que no es entero (th=abc) también responde <Error> en XML, no ProblemDetails.
        opciones.InvalidModelStateResponseFactory = contexto => RespuestasError.Crear(
            StatusCodes.Status400BadRequest, $"Parámetros inválidos: {string.Join(", ", contexto.ModelState.Keys)}.");
    });

var app = builder.Build();

// Rutas inexistentes o métodos no permitidos (cuerpo vacío) y excepciones sin controlar también salen como <Error>.
app.UseExceptionHandler(rama => rama.Run(contexto =>
    RespuestasError.EscribirAsync(contexto, StatusCodes.Status500InternalServerError, "Error interno del servidor.")));
app.UseStatusCodePages(contexto =>
    RespuestasError.EscribirAsync(contexto.HttpContext, contexto.HttpContext.Response.StatusCode, "No se pudo atender la petición."));

// Las pruebas lo apagan para no tocar la BD real; en ejecución normal la Api puede arrancar antes que la App 1.
if (app.Configuration.GetValue("Simulador:CrearBaseDatosAlArrancar", true))
{
    using var alcance = app.Services.CreateScope();
    await InicializadorBaseDatos.AsegurarCreadaAsync(alcance.ServiceProvider.GetRequiredService<SimuladorDbContext>());
}

app.MapControllers();

app.Run();

/// <summary>Expuesto para WebApplicationFactory en las pruebas.</summary>
public partial class Program;
