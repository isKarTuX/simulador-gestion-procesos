using SimuladorProcesos.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSimuladorData(builder.Configuration.GetConnectionString("Simulador"));

// El enunciado exige XML: se registran los formateadores XmlSerializer y cada controlador declara [Produces("application/xml")].
builder.Services.AddControllers().AddXmlSerializerFormatters();

var app = builder.Build();

app.MapControllers();

app.Run();

/// <summary>Expuesto para WebApplicationFactory en las pruebas.</summary>
public partial class Program;
