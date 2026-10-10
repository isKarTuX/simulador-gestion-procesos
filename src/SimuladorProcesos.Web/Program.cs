using SimuladorProcesos.Engine;
using SimuladorProcesos.Web.Components;
using SimuladorProcesos.Web.Servicios;
using SimuladorProcesos.Web.Simulacion;

var builder = WebApplication.CreateBuilder(args);

// Blazor Server: el circuito SignalR de cada pestaña lleva las actualizaciones del motor en vivo (ADR-0001).
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5080";
builder.Services.AddHttpClient<IClienteCatalogos, ClienteCatalogos>(cliente => cliente.BaseAddress = new Uri(apiBaseUrl));

// Un simulador por circuito (pestaña). La subtarea 2.3 cambia SimuladorFalso por el motor real.
builder.Services.AddScoped<ISimulador, SimuladorFalso>();

var app = builder.Build();

app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();
