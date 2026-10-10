using System.IO;
using SimuladorProcesos.Desktop.Servicios;

namespace SimuladorProcesos.Desktop.Tests.Actividad;

public class NombreArchivoActividadTests
{
    [Theory]
    [InlineData("notepad", 10, "notepad_10.txt")]
    [InlineData("chrome.exe", 4321, "chrome_4321.txt")]
    [InlineData("CHROME.EXE", 7, "CHROME_7.txt")]
    [InlineData("my.app.exe", 7, "my.app_7.txt")]
    [InlineData("a.exe.exe", 7, "a.exe_7.txt")]
    public void Quita_la_extension_exe_solo_al_final(string nombre, int pid, string esperado) =>
        Assert.Equal(esperado, NombreArchivoActividad.Para(nombre, pid));

    [Theory]
    [InlineData("a/b\\c:d*e?f\"g<h>i|j", "abcdefghij_1.txt")]
    [InlineData("  nombre  ", "nombre_1.txt")]
    [InlineData("nombre. .", "nombre_1.txt")]
    public void Quita_caracteres_invalidos_y_bordes(string nombre, string esperado) =>
        Assert.Equal(esperado, NombreArchivoActividad.Para(nombre, 1));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("???")]
    [InlineData(".exe")]
    public void Un_nombre_vacio_queda_como_proceso(string? nombre) =>
        Assert.Equal("proceso_5.txt", NombreArchivoActividad.Para(nombre, 5));

    [Theory]
    [InlineData("CON", "CON__9.txt")]
    [InlineData("nul", "nul__9.txt")]
    [InlineData("Com1", "Com1__9.txt")]
    [InlineData("LPT9.exe", "LPT9__9.txt")]
    [InlineData("aux.tar", "aux_.tar_9.txt")]
    public void Los_nombres_reservados_de_windows_llevan_sufijo(string nombre, string esperado) =>
        Assert.Equal(esperado, NombreArchivoActividad.Para(nombre, 9));

    [Theory]
    [InlineData("COM0")]
    [InlineData("CONSOLE")]
    [InlineData("LPT10")]
    public void Los_nombres_parecidos_a_reservados_no_cambian(string nombre) =>
        Assert.Equal($"{nombre}_9.txt", NombreArchivoActividad.Para(nombre, 9));

    [Fact]
    public void El_pid_distingue_procesos_con_el_mismo_nombre() =>
        Assert.NotEqual(NombreArchivoActividad.Para("svchost.exe", 100), NombreArchivoActividad.Para("svchost.exe", 200));

    [Fact]
    public void Un_nombre_con_separadores_de_ruta_no_puede_escapar_de_la_carpeta()
    {
        var resultado = NombreArchivoActividad.Para("..\\..\\Windows/System32\\cmd.exe", 3);

        Assert.Equal("WindowsSystem32cmd_3.txt", resultado);
        Assert.Equal(-1, resultado.IndexOfAny(Path.GetInvalidFileNameChars()));
    }
}
