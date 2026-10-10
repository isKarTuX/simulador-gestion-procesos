using SimuladorProcesos.Core.Modelos;
using SimuladorProcesos.Engine.Actividad;
using SimuladorProcesos.Engine.Tests.Planificacion;
using static SimuladorProcesos.Engine.Tests.Concurrencia.AuxiliarSimulador;

namespace SimuladorProcesos.Engine.Tests.Concurrencia.QA;

/// <summary>QA adversarial: manejadores de archivo abiertos por DestinoArchivos (solo Linux, se leen los descriptores del proceso).</summary>
[Trait("Category", "Concurrencia")]
public class ArchivosAbiertosTests
{
    private static string NuevaCarpeta() => Path.Combine(Path.GetTempPath(), "simulador-engine-tests", "qa-" + Guid.NewGuid().ToString("N"));

    // Cuenta los descriptores de este proceso que apuntan a archivos dentro de la carpeta.
    private static int AbiertosEn(string carpeta)
    {
        var abiertos = 0;
        var descriptores = Directory.GetFiles(DirectorioDescriptores());
        foreach (var descriptor in descriptores)
        {
            var destino = new FileInfo(descriptor).LinkTarget;
            if (destino is not null && destino.StartsWith(carpeta, StringComparison.Ordinal))
            {
                abiertos++;
            }
        }

        return abiertos;
    }

    private static string DirectorioDescriptores() => Path.Combine("/", "proc", "self", "fd");

    [Fact]
    public void Tras_Reiniciar_no_queda_ningun_archivo_de_actividad_abierto()
    {
        if (!OperatingSystem.IsLinux())
        {
            return;
        }

        var carpeta = NuevaCarpeta();
        using var destino = new DestinoArchivos(carpeta);
        ProcesoEntrada[] procesos =
        [
            AuxiliarPlanificador.Proceso("Ana", Texto(400), Prioridad.Expulsivo),
            AuxiliarPlanificador.Proceso("Beto", Texto(400), Prioridad.NoExpulsivo),
        ];
        using var simulador = Cargado(destino, procesos, 10);

        simulador.Iniciar();
        Esperar(() => AbiertosEn(carpeta) != 0, "Ningún proceso llegó a abrir su archivo.");

        simulador.Reiniciar();

        // Detenida no ejecuta nada: ningún archivo debería seguir abierto (en Windows impediría borrar o mover la carpeta).
        Assert.Equal(0, AbiertosEn(carpeta));
    }
}
