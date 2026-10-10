namespace SimuladorProcesos.Engine.Actividad;

/// <summary>
/// Nombre del archivo de actividad de un proceso: "&lt;nombre saneado&gt;_&lt;pid&gt;.txt".
/// Repite el criterio de la App 1 (Desktop) porque Engine no puede referenciarla (AGENTS.md, regla 2).
/// </summary>
internal static class NombreArchivoActividad
{
    // Se une a los de la plataforma para sanear igual en Linux y en Windows (Linux solo prohíbe '/' y '\0').
    private static readonly HashSet<char> _caracteresInvalidos =
        [.. Path.GetInvalidFileNameChars(), .. "<>:\"/\\|?*"];

    private static readonly HashSet<string> _reservados = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
    };

    /// <summary>El pid evita que dos procesos con el mismo nombre (svchost, chrome) sobrescriban el mismo archivo.</summary>
    public static string Para(string? nombre, int pid) => $"{Sanear(nombre)}_{pid}.txt";

    private static string Sanear(string? nombre)
    {
        var limpio = (nombre ?? string.Empty).Trim();

        // Solo se quita el .exe final: el archivo de actividad es siempre .txt.
        if (limpio.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            limpio = limpio[..^4];
        }

        limpio = string.Concat(limpio.Where(c => !_caracteresInvalidos.Contains(c)));

        // Windows descarta espacios y puntos finales, y dejarían el nombre distinto del pedido.
        limpio = limpio.Trim(' ', '.');

        if (limpio.Length == 0)
        {
            return "proceso";
        }

        // Windows reserva el nombre antes del primer punto ("NUL.tar" también lo es), así que el sufijo va ahí.
        var punto = limpio.IndexOf('.');
        var raiz = punto < 0 ? limpio : limpio[..punto];
        return _reservados.Contains(raiz) ? raiz + "_" + limpio[raiz.Length..] : limpio;
    }
}
