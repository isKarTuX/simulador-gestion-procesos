using SimuladorProcesos.Engine;
using SimuladorProcesos.Web.Components;
using SimuladorProcesos.Web.Servicios;
using SimuladorProcesos.Web.Simulacion;

var builder = WebApplication.CreateBuilder(args);

// Blazor Server: el circuito SignalR de cada pestaña lleva las actualizaciones del motor en vivo (ADR-0001).
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5080";
builder.Services.AddHttpClient<IClienteCatalogos, ClienteCatalogos>(cliente => cliente.BaseAddress = new Uri(apiBaseUrl));

// Un simulador por circuito (pestaña): el contenedor lo libera con el circuito.
// Motor real por defecto; "UsarSimuladorFalso": true solo para demos sin la Api.
if (builder.Configuration.GetValue<bool>("UsarSimuladorFalso"))
{
    builder.Services.AddScoped<ISimulador, SimuladorFalso>();
}
else
{
    builder.Services.AddScoped<ISimulador, SimuladorRoundRobin>();
}

var app = builder.Build();

app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();
