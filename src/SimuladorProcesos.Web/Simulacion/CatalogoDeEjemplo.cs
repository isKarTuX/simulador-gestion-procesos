using SimuladorProcesos.Core.Modelos;
using SimuladorProcesos.Engine;

namespace SimuladorProcesos.Web.Simulacion;

/// <summary>Catálogo fijo para probar la GUI sin la Api: mezcla prioridades y largos de descripción.</summary>
public static class CatalogoDeEjemplo
{
    public const string Nombre = "Catálogo de ejemplo";

    // Unos 150 caracteres: con TH = 100 ms la simulación dura cerca de 15 s, suficiente para pausar y reiniciar.
    public static IReadOnlyList<ProcesoEntrada> Procesos { get; } =
    [
        new(4120, "WINWORD", "Procesador de texto Word", Prioridad.Expulsivo),
        new(4, "System", "Kernel del sistema", Prioridad.NoExpulsivo),
        new(7312, "chrome", "Navegador web con varias pestañas abiertas", Prioridad.Expulsivo),
        new(880, "svchost", "Host de servicios", Prioridad.NoExpulsivo),
        new(5560, "notepad", "Bloc de notas", Prioridad.Expulsivo),
        new(1024, "explorer", "Explorador de archivos de Windows", Prioridad.Expulsivo),
    ];
}
