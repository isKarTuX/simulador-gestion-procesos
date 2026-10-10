using System.IO;
using System.Text;
using SimuladorProcesos.Core.Modelos;
using SimuladorProcesos.Desktop.Servicios;

namespace SimuladorProcesos.Desktop.Tests.Actividad;

public sealed class ActividadProcesoTests : IDisposable
{
    private readonly string _carpeta = Path.Combine(Path.GetTempPath(), "actividad-" + Guid.NewGuid().ToString("N"));
    private readonly ActividadProceso _actividad = new();

    public void Dispose()
    {
        if (Directory.Exists(_carpeta))
        {
            Directory.Delete(_carpeta, recursive: true);
        }
    }

    private static ProcesoCatalogo Proceso(string descripcion, string nombre = "notepad.exe", int pid = 10) =>
        new() { Nombre = nombre, Pid = pid, Descripcion = descripcion };

    [Fact]
    public async Task Copia_la_descripcion_completa_con_tildes_y_enie()
    {
        const string descripcion = "Editor de texto: ñandú, canción, ÁÉÍÓÚ, ü";

        var ruta = await _actividad.EjecutarAsync(Proceso(descripcion), 1, _carpeta);

        Assert.Equal(Path.Combine(_carpeta, "notepad_10.txt"), ruta);
        Assert.Equal(descripcion, await File.ReadAllTextAsync(ruta, Encoding.UTF8));
    }

    [Fact]
    public async Task Escribe_utf8_sin_bom()
    {
        var ruta = await _actividad.EjecutarAsync(Proceso("ñ"), 1, _carpeta);

        Assert.Equal(new byte[] { 0xC3, 0xB1 }, await File.ReadAllBytesAsync(ruta));
    }

    [Fact]
    public async Task Un_par_sustituto_partido_en_dos_caracteres_sale_como_utf8_valido()
    {
        const string descripcion = "a😀b";

        var ruta = await _actividad.EjecutarAsync(Proceso(descripcion), 1, _carpeta);

        Assert.Equal(descripcion, await File.ReadAllTextAsync(ruta, Encoding.UTF8));
    }

    [Fact]
    public async Task Crea_la_carpeta_si_no_existe()
    {
        var anidada = Path.Combine(_carpeta, "a", "b");

        var ruta = await _actividad.EjecutarAsync(Proceso("x"), 1, anidada);

        Assert.True(File.Exists(ruta));
    }

    [Fact]
    public async Task Reporta_el_progreso_de_1_a_N_en_orden()
    {
        const string descripcion = "Hola ñandú";
        var progreso = new ProgresoSincrono();

        await _actividad.EjecutarAsync(Proceso(descripcion), 1, _carpeta, progreso);

        Assert.Equal(Enumerable.Range(1, descripcion.Length), progreso.Valores);
    }

    [Fact]
    public async Task Cada_caracter_ya_esta_en_disco_cuando_se_reporta_y_el_archivo_se_puede_leer_mientras_se_escribe()
    {
        const string descripcion = "abcdef";
        var vistos = new List<string>();
        var progreso = new ProgresoSincrono(_ =>
        {
            // Sin FileShare.Read esta apertura fallaría; sin Flush vería menos texto del reportado.
            var ruta = Path.Combine(_carpeta, "notepad_10.txt");
            using var lector = new FileStream(ruta, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var texto = new StreamReader(lector, Encoding.UTF8);
            vistos.Add(texto.ReadToEnd());
        });

        await _actividad.EjecutarAsync(Proceso(descripcion), 1, _carpeta, progreso);

        Assert.Equal(["a", "ab", "abc", "abcd", "abcde", "abcdef"], vistos);
    }

    [Fact]
    public async Task Descripcion_vacia_crea_un_archivo_vacio_sin_progreso()
    {
        var progreso = new ProgresoSincrono();

        var ruta = await _actividad.EjecutarAsync(Proceso(string.Empty), 1, _carpeta, progreso);

        Assert.True(File.Exists(ruta));
        Assert.Equal(0, new FileInfo(ruta).Length);
        Assert.Empty(progreso.Valores);
    }

    [Fact]
    public async Task Usa_el_nombre_saneado_con_el_pid()
    {
        var ruta = await _actividad.EjecutarAsync(Proceso("x", nombre: "chrome.exe", pid: 4321), 1, _carpeta);

        Assert.Equal("chrome_4321.txt", Path.GetFileName(ruta));
    }

    [Fact]
    public async Task Dos_procesos_con_el_mismo_nombre_generan_archivos_distintos()
    {
        var uno = await _actividad.EjecutarAsync(Proceso("uno", "svchost.exe", 100), 1, _carpeta);
        var dos = await _actividad.EjecutarAsync(Proceso("dos", "svchost.exe", 200), 1, _carpeta);

        Assert.Equal("uno", await File.ReadAllTextAsync(uno));
        Assert.Equal("dos", await File.ReadAllTextAsync(dos));
    }

    [Fact]
    public async Task Cancelar_a_mitad_deja_el_archivo_parcial_y_propaga_la_cancelacion()
    {
        const string descripcion = "0123456789";
        using var fuente = new CancellationTokenSource();
        var progreso = new ProgresoSincrono(copiados =>
        {
            if (copiados == 4)
            {
                fuente.Cancel();
            }
        });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _actividad.EjecutarAsync(Proceso(descripcion), 1, _carpeta, progreso, fuente.Token));

        Assert.Equal("0123", await File.ReadAllTextAsync(Path.Combine(_carpeta, "notepad_10.txt")));
        Assert.Equal([1, 2, 3, 4], progreso.Valores);
    }

    [Fact]
    public async Task Cancelar_antes_de_empezar_no_crea_el_archivo()
    {
        using var fuente = new CancellationTokenSource();
        await fuente.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _actividad.EjecutarAsync(Proceso("abc"), 1, _carpeta, cancelacion: fuente.Token));

        Assert.False(Directory.Exists(_carpeta));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task Un_th_no_positivo_es_un_argumento_invalido(int th)
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _actividad.EjecutarAsync(Proceso("abc"), th, _carpeta));

        Assert.False(Directory.Exists(_carpeta));
    }

    [Fact]
    public async Task Espera_th_entre_caracteres()
    {
        var reloj = System.Diagnostics.Stopwatch.StartNew();

        await _actividad.EjecutarAsync(Proceso("abcde"), 30, _carpeta);

        // 4 esperas de 30 ms entre 5 caracteres; el margen cubre que los temporizadores no son exactos.
        Assert.True(reloj.ElapsedMilliseconds >= 100, $"Tardó {reloj.ElapsedMilliseconds} ms");
    }

    /// <summary>IProgress síncrono: Progress&lt;T&gt; publica en otro hilo y el orden de las pruebas no sería determinista.</summary>
    private sealed class ProgresoSincrono(Action<int>? alReportar = null) : IProgress<int>
    {
        public List<int> Valores { get; } = [];

        public void Report(int value)
        {
            Valores.Add(value);
            alReportar?.Invoke(value);
        }
    }
}
