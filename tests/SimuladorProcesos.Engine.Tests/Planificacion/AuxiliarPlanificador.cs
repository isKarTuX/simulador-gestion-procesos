using System.Text;
using SimuladorProcesos.Core.Modelos;
using SimuladorProcesos.Engine.Planificacion;

namespace SimuladorProcesos.Engine.Tests.Planificacion;

/// <summary>Ayudas compartidas por las pruebas del planificador; no contienen lógica de planificación.</summary>
internal static class AuxiliarPlanificador
{
    public const Prioridad Expulsivo = Prioridad.Expulsivo;
    public const Prioridad NoExpulsivo = Prioridad.NoExpulsivo;

    public static ProcesoEntrada Proceso(string nombre, string descripcion, Prioridad prioridad, int? quantumPropio = null) =>
        new(nombre[0], nombre, descripcion, prioridad, quantumPropio);

    public static PlanificadorRoundRobin Crear(int thMs, int quantum, List<string> registro, params ProcesoEntrada[] procesos) =>
        new(procesos, new ConfiguracionSimulacion(thMs, quantum), e => registro.Add(Formato(e)));

    /// <summary>Una línea por evento, "A: Listo > Ejecucion @20"; "nuevo" es la llegada (desde null).</summary>
    public static string Formato(CambioEstadoEventArgs e) =>
        $"{e.Proceso.Nombre}: {e.Desde?.ToString() ?? "nuevo"} > {e.Hacia} @{e.RelojMs}";

    /// <summary>Hace un turno completo como lo hará el hilo del proceso y devuelve "Nombre:caracteres copiados".</summary>
    public static string EjecutarTurno(PlanificadorRoundRobin planificador)
    {
        var proceso = planificador.Despachar() ?? throw new InvalidOperationException("No había nadie en Listos.");
        var texto = new StringBuilder();
        for (var restantes = planificador.LimiteDelTurno(proceso); restantes > 0; restantes--)
        {
            texto.Append(planificador.CopiarCaracter());
        }

        planificador.FinalizarTurno();
        return $"{proceso.Nombre}:{texto}";
    }

    public static List<string> EjecutarTodo(PlanificadorRoundRobin planificador)
    {
        var turnos = new List<string>();
        while (!planificador.TodosTerminados)
        {
            // El tope evita que un error de planificación cuelgue la prueba en vez de hacerla fallar.
            Assert.True(turnos.Count < 10_000, "La simulación no termina.");
            turnos.Add(EjecutarTurno(planificador));
        }

        return turnos;
    }

    /// <summary>Terminados en orden, "B@40": nombre y reloj simulado en que terminó.</summary>
    public static List<string> Finalizaciones(PlanificadorRoundRobin planificador) =>
        planificador.Terminados.Select(p => $"{p.Nombre}@{p.TiempoFinalizacionReal}").ToList();
}
