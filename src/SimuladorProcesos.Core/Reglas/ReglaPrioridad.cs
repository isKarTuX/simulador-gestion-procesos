using SimuladorProcesos.Core.Modelos;

namespace SimuladorProcesos.Core.Reglas;

/// <summary>Procesos generados por el SO son no expulsivos; el resto, expulsivos (CONTEXT §6-M1).</summary>
public static class ReglaPrioridad
{
    private static readonly string[] _cuentasDelSistema =
    [
        "SYSTEM",
        "LOCAL SERVICE",
        "NETWORK SERVICE",
        "SERVICIO LOCAL",
        "SERVICIO DE RED",
    ];

    public static Prioridad Determinar(string? usuario)
    {
        if (string.IsNullOrWhiteSpace(usuario))
        {
            // Sin dueño visible: lo trata como proceso del SO (suele ser un proceso protegido).
            return Prioridad.NoExpulsivo;
        }

        // "DOMINIO\cuenta" → "cuenta"
        var cuenta = usuario[(usuario.LastIndexOf('\\') + 1)..];
        var esDelSistema = usuario.StartsWith("NT AUTHORITY", StringComparison.OrdinalIgnoreCase)
            || _cuentasDelSistema.Contains(cuenta.Trim(), StringComparer.OrdinalIgnoreCase);

        return esDelSistema ? Prioridad.NoExpulsivo : Prioridad.Expulsivo;
    }
}
