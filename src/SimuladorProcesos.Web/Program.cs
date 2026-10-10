using SimuladorProcesos.Web.Components;

var builder = WebApplication.CreateBuilder(args);

// Blazor Server: el circuito SignalR de cada pestaña lleva las actualizaciones del motor en vivo (ADR-0001).
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

var app = builder.Build();

app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();
