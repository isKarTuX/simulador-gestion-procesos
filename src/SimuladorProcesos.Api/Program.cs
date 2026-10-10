using Microsoft.EntityFrameworkCore;
using SimuladorProcesos.Core;
using SimuladorProcesos.Data;

var builder = WebApplication.CreateBuilder(args);

var cadenaConexion = builder.Configuration.GetConnectionString("Simulador") ?? RutasSimulador.CadenaConexionPorDefecto;
builder.Services.AddDbContext<SimuladorDbContext>(opciones => opciones.UseSqlite(cadenaConexion));

// El enunciado exige XML: se registran los formateadores XmlSerializer y cada controlador declara [Produces("application/xml")].
builder.Services.AddControllers().AddXmlSerializerFormatters();

var app = builder.Build();

app.MapControllers();

app.Run();

/// <summary>Expuesto para WebApplicationFactory en las pruebas.</summary>
public partial class Program;
