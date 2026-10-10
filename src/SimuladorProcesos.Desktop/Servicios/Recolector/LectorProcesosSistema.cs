using System.ComponentModel;
using System.Diagnostics;
using SimuladorProcesos.Core.Modelos;

namespace SimuladorProcesos.Desktop.Servicios.Recolector;

/// <summary>Lee los procesos reales con System.Diagnostics.Process.</summary>
public sealed class LectorProcesosSistema : ILectorProcesos
{
    private static readonly TimeSpan _intervaloPorDefecto = TimeSpan.FromMilliseconds(500);

    private readonly TimeSpan _intervaloCpu;

    public LectorProcesosSistema(TimeSpan? intervaloCpu = null)
    {
        _intervaloCpu = intervaloCpu ?? _intervaloPorDefecto;
    }

    public async Task<IReadOnlyList<MuestraProceso>> LeerAsync(CriterioSeleccion criterio, CancellationToken cancelacion = default)
    {
        // GetProcesses y las lecturas pueden tardar con cientos de procesos: fuera del hilo de la UI.
        var procesos = await Task.Run(ObtenerProcesos, cancelacion);
        try
        {
            if (criterio == CriterioSeleccion.Memoria)
            {
                return procesos
                    .Select(p => new MuestraProceso(p.Id, NombreDe(p), Leer(() => p.WorkingSet64)))
                    .ToList();
            }

            // Se conservan los Process abiertos durante la espera para leer el mismo proceso dos veces (el PID podría reutilizarse).
            var iniciales = procesos.Select(p => Leer(() => p.TotalProcessorTime)).ToList();
            await Task.Delay(_intervaloCpu, cancelacion);

            return procesos
                .Select((p, i) => new MuestraProceso(p.Id, NombreDe(p), null, iniciales[i], Leer(() => p.TotalProcessorTime)))
                .ToList();
        }
        finally
        {
            foreach (var proceso in procesos)
            {
                proceso.Dispose();
            }
        }
    }

    private static List<Process> ObtenerProcesos()
    {
        var actual = Environment.ProcessId;
        var todos = Process.GetProcesses();
        var utiles = new List<Process>(todos.Length);

        foreach (var proceso in todos)
        {
            // PID 0 es Idle (no es un proceso real) y el propio simulador no debe aparecer en su catálogo.
            if (proceso.Id == 0 || proceso.Id == actual)
            {
                proceso.Dispose();
            }
            else
            {
                utiles.Add(proceso);
            }
        }

        return utiles;
    }

    private static string NombreDe(Process proceso)
    {
        try
        {
            return proceso.ProcessName;
        }
        catch (Exception ex) when (ex is InvalidOperationException or Win32Exception)
        {
            return string.Empty;
        }
    }

    // Access Denied (Win32Exception) o proceso terminado (InvalidOperationException): el dato queda en null.
    private static T? Leer<T>(Func<T> lectura)
        where T : struct
    {
        try
        {
            return lectura();
        }
        catch (Exception ex) when (ex is InvalidOperationException or Win32Exception or NotSupportedException)
        {
            return null;
        }
    }
}
