namespace SimuladorProcesos.Desktop.Servicios;

/// <summary>Usuario dueño y descripción de un proceso del SO.</summary>
public sealed record MetadatosProceso(string Usuario, string Descripcion);

/// <summary>M1: lee metadatos de un proceso. Implementa: subtarea 1.4 en Servicios/Metadatos.</summary>
public interface IProveedorMetadatos
{
    /// <summary>
    /// Nunca lanza. Si no puede leer el usuario devuelve "" (ReglaPrioridad lo trata como proceso del SO);
    /// si no puede leer la descripción devuelve <paramref name="nombreProceso"/>. Devuelve null solo si el proceso ya terminó.
    /// </summary>
    MetadatosProceso? Obtener(int pid, string nombreProceso);
}
