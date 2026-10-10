using SimuladorProcesos.Core.Modelos;
using SimuladorProcesos.Core.Reglas;
using SimuladorProcesos.Desktop.Servicios;

namespace SimuladorProcesos.Desktop.Falsos;

/// <summary>Recolector de mentira: devuelve procesos inventados (mezcla de SYSTEM y usuario) para probar la ventana sin tocar el SO.</summary>
public sealed class RecolectorFalso : IRecolectorProcesos
{
    private const string _usuarioHumano = "PC-DEMO\\estudiante";

    // Ordenados por "uso de CPU" descendente; para memoria se usa otro orden fijo (alfabético) y así los dos criterios dan listas distintas.
    private static readonly (string Nombre, string Usuario, string Descripcion)[] _catalogo =
    [
        ("System", "NT AUTHORITY\\SYSTEM", "Kernel y hilos del sistema"),
        ("chrome", _usuarioHumano, "Google Chrome"),
        ("Code", _usuarioHumano, "Visual Studio Code"),
        ("dwm", "NT AUTHORITY\\SYSTEM", "Administrador de ventanas del escritorio"),
        ("MsMpEng", "NT AUTHORITY\\SYSTEM", "Antimalware Service Executable"),
        ("explorer", _usuarioHumano, "Explorador de Windows"),
        ("svchost", "NT AUTHORITY\\NETWORK SERVICE", "Proceso host para servicios de Windows"),
        ("devenv", _usuarioHumano, "Microsoft Visual Studio"),
        ("csrss", "NT AUTHORITY\\SYSTEM", "Proceso de ejecución cliente-servidor"),
        ("Teams", _usuarioHumano, "Microsoft Teams"),
        ("lsass", "NT AUTHORITY\\SYSTEM", "Proceso de la autoridad de seguridad local"),
        ("spoolsv", "NT AUTHORITY\\SYSTEM", "Administrador de cola de impresión"),
        ("Spotify", _usuarioHumano, "Spotify"),
        ("winlogon", "NT AUTHORITY\\SYSTEM", "Aplicación de inicio de sesión de Windows"),
        ("OneDrive", _usuarioHumano, "Microsoft OneDrive"),
        ("services", "NT AUTHORITY\\SYSTEM", "Administrador de control de servicios"),
        ("SearchHost", _usuarioHumano, "Búsqueda de Windows"),
        ("wininit", "NT AUTHORITY\\SYSTEM", "Aplicación de inicio de Windows"),
        ("notepad", _usuarioHumano, "Bloc de notas"),
        ("conhost", _usuarioHumano, "Host de ventana de consola"),
        ("audiodg", "NT AUTHORITY\\LOCAL SERVICE", "Aislamiento de gráficos de dispositivos de audio"),
        ("taskhostw", _usuarioHumano, "Proceso host para tareas de Windows"),
        ("smss", "NT AUTHORITY\\SYSTEM", "Administrador de sesión de Windows"),
        ("WindowsTerminal", _usuarioHumano, "Terminal de Windows"),
    ];

    private readonly TimeSpan _latencia;

    /// <param name="latencia">Pausa que simula lo que tarda la captura real (dos muestras de CPU); las pruebas pasan cero.</param>
    public RecolectorFalso(TimeSpan? latencia = null) => _latencia = latencia ?? TimeSpan.FromMilliseconds(600);

    public async Task<IReadOnlyList<ProcesoCatalogo>> CapturarAsync(
        int cantidad,
        CriterioSeleccion criterio,
        CancellationToken cancelacion = default)
    {
        await Task.Delay(_latencia, cancelacion);

        var origen = criterio == CriterioSeleccion.Cpu
            ? _catalogo
            : [.. _catalogo.OrderBy(p => p.Nombre, StringComparer.OrdinalIgnoreCase)];
        return [.. origen
            .Take(Math.Max(cantidad, 0))
            .Select(p => new ProcesoCatalogo
            {
                Pid = 400 + (Array.IndexOf(_catalogo, p) * 4),
                Nombre = p.Nombre,
                Usuario = p.Usuario,
                Descripcion = p.Descripcion,
                Prioridad = ReglaPrioridad.Determinar(p.Usuario),
            })];
    }
}
