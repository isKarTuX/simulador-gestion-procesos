using System.Text;
using SimuladorProcesos.Core.Modelos;
using SimuladorProcesos.Engine.Actividad;
using SimuladorProcesos.Engine.Tests.Planificacion;
using static SimuladorProcesos.Engine.Tests.Concurrencia.AuxiliarSimulador;

namespace SimuladorProcesos.Engine.Tests.Concurrencia;

/// <summary>Destinos de la actividad: nombre de archivo (mismo criterio que la App 1), contenido exacto y limpieza entre corridas.</summary>
public class DestinosActividadTests : IDisposable
{
    private readonly string _carpeta = Path.Combine(Path.GetTempPath(), "simulador-engine-tests", Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_carpeta))
        {
            Directory.Delete(_carpeta, recursive: true);
        }
    }

    // Se lee con FileShare.ReadWrite|Delete como un lector real: File.ReadAllText usa FileShare.Read y en Windows choca con el escritor abierto.
    private static byte[] LeerBytes(string ruta)
    {
        using var flujo = new FileStream(ruta, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var memoria = new MemoryStream();
        flujo.CopyTo(memoria);
        return memoria.ToArray();
    }

    private static string LeerTexto(string ruta) => Encoding.UTF8.GetString(LeerBytes(ruta));

    [Theory]
    [InlineData("notepad", 10, "notepad_10.txt")]
    [InlineData("chrome.exe", 4321, "chrome_4321.txt")]
    [InlineData("CHROME.EXE", 7, "CHROME_7.txt")]
    [InlineData("a.exe.exe", 7, "a.exe_7.txt")]
    [InlineData("a/b\\c:d*e?f\"g<h>i|j", 1, "abcdefghij_1.txt")]
    [InlineData("  nombre  ", 1, "nombre_1.txt")]
    [InlineData("nombre. .", 1, "nombre_1.txt")]
    [InlineData("", 5, "proceso_5.txt")]
    [InlineData("???", 5, "proceso_5.txt")]
    [InlineData(".exe", 5, "proceso_5.txt")]
    [InlineData("CON", 9, "CON__9.txt")]
    [InlineData("LPT9.exe", 9, "LPT9__9.txt")]
    [InlineData("aux.tar", 9, "aux_.tar_9.txt")]
    [InlineData("CONSOLE", 9, "CONSOLE_9.txt")]
    public void El_nombre_del_archivo_sigue_el_criterio_de_la_App_1(string nombre, int pid, string esperado) =>
        Assert.Equal(esperado, NombreArchivoActividad.Para(nombre, pid));

    [Fact]
    public void El_nombre_nulo_queda_como_proceso_y_el_pid_distingue_procesos_con_el_mismo_nombre()
    {
        Assert.Equal("proceso_5.txt", NombreArchivoActividad.Para(null, 5));
        Assert.NotEqual(NombreArchivoActividad.Para("svchost.exe", 100), NombreArchivoActividad.Para("svchost.exe", 200));
    }

    [Fact]
    public void DestinoArchivos_escribe_un_archivo_por_proceso_con_la_descripcion_exacta_y_sin_BOM()
    {
        using var destino = new DestinoArchivos(_carpeta);

        foreach (var c in "hola") { destino.Escribir(1, "uno.exe", c); }
        foreach (var c in "ñandú") { destino.Escribir(2, "dos", c); }

        Assert.Equal("hola", LeerTexto(Path.Combine(_carpeta, "uno_1.txt")));
        Assert.Equal(Encoding.UTF8.GetBytes("ñandú"), LeerBytes(Path.Combine(_carpeta, "dos_2.txt")));
    }

    [Fact]
    public void DestinoArchivos_conserva_los_pares_sustitutos_escritos_de_a_un_char()
    {
        const string descripcion = "a😀b";
        using var destino = new DestinoArchivos(_carpeta);

        foreach (var c in descripcion) { destino.Escribir(3, "emoji", c); }

        Assert.Equal(Encoding.UTF8.GetBytes(descripcion), LeerBytes(Path.Combine(_carpeta, "emoji_3.txt")));
    }

    [Fact]
    public void DestinoArchivos_deja_leer_el_archivo_mientras_todavia_se_escribe()
    {
        using var destino = new DestinoArchivos(_carpeta);

        destino.Escribir(4, "vivo", 'x');
        destino.Escribir(4, "vivo", 'y');

        Assert.Equal("xy", LeerTexto(Path.Combine(_carpeta, "vivo_4.txt")));
    }

    [Fact]
    public void DestinoArchivos_empieza_de_cero_en_cada_corrida()
    {
        using var destino = new DestinoArchivos(_carpeta);
        foreach (var c in "viejo texto") { destino.Escribir(1, "p", c); }

        destino.NuevaCorrida();
        foreach (var c in "nuevo") { destino.Escribir(1, "p", c); }

        Assert.Equal("nuevo", LeerTexto(Path.Combine(_carpeta, "p_1.txt")));
    }

    [Fact]
    public void DestinoArchivos_exige_una_carpeta()
    {
        Assert.Throws<ArgumentException>(() => new DestinoArchivos(" "));
        Assert.Throws<ArgumentNullException>(() => new DestinoArchivos(null!));
    }

    [Fact]
    public void DestinoEnMemoria_NuevaCorrida_borra_lo_escrito_y_cuenta_las_corridas()
    {
        var destino = new DestinoEnMemoria();
        destino.Escribir(1, "p", 'a');
        Assert.Equal("a", destino.Texto(1));

        destino.NuevaCorrida();

        Assert.Equal(string.Empty, destino.Texto(1));
        Assert.Equal(1, destino.Corridas);
    }

    [Fact]
    [Trait("Category", "Concurrencia")]
    public void El_simulador_con_DestinoArchivos_deja_un_txt_por_proceso_con_su_descripcion()
    {
        var procesos = new[]
        {
            AuxiliarPlanificador.Proceso("Ana.exe", "abcdef", Prioridad.Expulsivo),
            AuxiliarPlanificador.Proceso("Beto", "ghij😀", Prioridad.NoExpulsivo),
            AuxiliarPlanificador.Proceso("Carla", string.Empty, Prioridad.Expulsivo),
        };
        using (var destino = new DestinoArchivos(_carpeta))
        using (var simulador = Cargado(destino, procesos, 2))
        {
            EjecutarHastaElFinal(simulador);
            simulador.Reiniciar();
            EjecutarHastaElFinal(simulador);
        }

        Assert.Equal("abcdef", LeerTexto(Path.Combine(_carpeta, NombreArchivoActividad.Para("Ana.exe", procesos[0].Pid))));
        Assert.Equal("ghij😀", LeerTexto(Path.Combine(_carpeta, NombreArchivoActividad.Para("Beto", procesos[1].Pid))));
        Assert.False(File.Exists(Path.Combine(_carpeta, NombreArchivoActividad.Para("Carla", procesos[2].Pid))));
    }
}
