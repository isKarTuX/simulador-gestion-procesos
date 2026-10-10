using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace SimuladorProcesos.Desktop.Servicios.Metadatos;

/// <summary>
/// Lee usuario (WMI Win32_Process.GetOwner) y descripción (FileVersionInfo) de un proceso.
/// Consulta WMI por PID y sin caché: el recolector solo pide metadatos de los N elegidos, no de todos.
/// </summary>
public sealed class ProveedorMetadatosWindows : IProveedorMetadatos
{
    private readonly record struct DatosWmi(string Usuario, string? RutaEjecutable);

    public MetadatosProceso? Obtener(int pid, string nombreProceso)
    {
        try
        {
            return ObtenerMetadatos(pid, nombreProceso);
        }
        catch (Exception)
        {
            // El contrato prohíbe lanzar: ante un fallo inesperado devuelve lo mínimo válido.
            return new MetadatosProceso("", nombreProceso);
        }
    }

    /// <summary>Une dominio y cuenta como "DOMINIO\usuario"; "" si GetOwner falló o no trajo cuenta.</summary>
    public static string ComponerUsuario(uint codigoRetorno, string? dominio, string? usuario)
    {
        if (codigoRetorno != 0 || string.IsNullOrWhiteSpace(usuario))
        {
            return "";
        }

        return string.IsNullOrWhiteSpace(dominio) ? usuario : $"{dominio}\\{usuario}";
    }

    private static MetadatosProceso? ObtenerMetadatos(int pid, string nombreProceso)
    {
        // WMI solo existe en Windows; en otros SO queda usuario vacío y se sigue con Process.
        var wmi = OperatingSystem.IsWindows() ? LeerWmi(pid) : null;
        using var proceso = AbrirProceso(pid);

        // Sin fila WMI y sin proceso vivo: terminó (o nunca existió).
        if (wmi is null && proceso is null)
        {
            return null;
        }

        var descripcion = DescripcionDelModulo(proceso);
        if (string.IsNullOrWhiteSpace(descripcion) && wmi?.RutaEjecutable is { } ruta)
        {
            // Respaldo cuando MainModule se niega (acceso denegado o proceso de otra arquitectura).
            descripcion = DescripcionDelArchivo(ruta);
        }

        return new MetadatosProceso(
            wmi?.Usuario ?? "",
            string.IsNullOrWhiteSpace(descripcion) ? nombreProceso : descripcion.Trim());
    }

    private static Process? AbrirProceso(int pid)
    {
        try
        {
            return Process.GetProcessById(pid);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return null;
        }
    }

    private static string? DescripcionDelModulo(Process? proceso)
    {
        try
        {
            using var modulo = proceso?.MainModule;
            return modulo?.FileVersionInfo.FileDescription;
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or NotSupportedException)
        {
            return null;
        }
    }

    private static string? DescripcionDelArchivo(string ruta)
    {
        try
        {
            return FileVersionInfo.GetVersionInfo(ruta).FileDescription;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return null;
        }
    }

    [SupportedOSPlatform("windows")]
    private static DatosWmi? LeerWmi(int pid)
    {
        try
        {
            var consulta = "SELECT ProcessId, ExecutablePath FROM Win32_Process WHERE ProcessId = "
                + pid.ToString(CultureInfo.InvariantCulture);
            using var buscador = new ManagementObjectSearcher(consulta);
            using var resultados = buscador.Get();
            foreach (var objeto in resultados)
            {
                using (objeto)
                {
                    return new DatosWmi(LeerUsuario(objeto), objeto["ExecutablePath"] as string);
                }
            }

            return null;
        }
        catch (Exception ex) when (ex is ManagementException or COMException or UnauthorizedAccessException or InvalidOperationException)
        {
            // WMI caído o sin permisos: se trata como "sin fila" y el PID se confirma con Process.
            return null;
        }
    }

    [SupportedOSPlatform("windows")]
    private static string LeerUsuario(ManagementBaseObject proceso)
    {
        try
        {
            using var salida = ((ManagementObject)proceso).InvokeMethod("GetOwner", null, null);
            if (salida is null || salida["ReturnValue"] is not uint codigo)
            {
                return "";
            }

            return ComponerUsuario(codigo, salida["Domain"] as string, salida["User"] as string);
        }
        catch (Exception ex) when (ex is ManagementException or COMException or UnauthorizedAccessException or InvalidOperationException)
        {
            return "";
        }
    }
}
