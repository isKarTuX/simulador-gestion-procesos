using SimuladorProcesos.Core.Modelos;
using SimuladorProcesos.Engine.Calculos;
using SimuladorProcesos.Engine.Modelo;

namespace SimuladorProcesos.Engine.Tests.Calculos;

public class CalculadoraTiemposTests
{
    private static readonly ConfiguracionSimulacion _configuracion = new(thMs: 10, quantum: 2);

    /// <summary>
    /// Catálogo a mano, TH = 10 ms, quantum = 2. Escenario Round Robin (orden de terminación C, B, A):
    ///   A "abcdef" (6 car.) expulsivo,    TL 0 : 0-20, 70-90, 110-130 -> 3 ejecuciones, termina en 130
    ///   B "abcd"   (4 car.) expulsivo,    TL 1 : 20-40, 90-110        -> 2 ejecuciones, termina en 110
    ///   C "abc"    (3 car.) no expulsivo, TL 2 : 40-70 de corrido     -> 1 ejecución,   termina en 70
    ///
    /// R = TH x caracteres ; T.F = quantum x ejecuciones ; T.R = TF real - TL ; Espera = T.R - R
    ///   P | T.L |  R | P.R | T.R | T.F | TF real | Espera
    ///   A |  0  | 60 |  0  | 130 |  6  |   130   |  70
    ///   B |  1  | 40 |  0  | 109 |  4  |   110   |  69
    ///   C |  2  | 30 |  1  |  68 |  2  |    70   |  38
    /// </summary>
    private static ProcesoSimulado[] CatalogoTerminadosEnOrden() =>
    [
        Terminado(3, "C", "abc", Prioridad.NoExpulsivo, tl: 2, ejecuciones: 1, tfReal: 70),
        Terminado(2, "B", "abcd", Prioridad.Expulsivo, tl: 1, ejecuciones: 2, tfReal: 110),
        Terminado(1, "A", "abcdef", Prioridad.Expulsivo, tl: 0, ejecuciones: 3, tfReal: 130),
    ];

    private static ProcesoSimulado Terminado(int numero, string nombre, string descripcion, Prioridad prioridad, long tl, int ejecuciones, long tfReal) =>
        new(numero, new ProcesoEntrada(numero * 10, nombre, descripcion, prioridad), _configuracion)
        {
            Estado = EstadoProceso.Terminado,
            TiempoLlegada = tl,
            Ejecuciones = ejecuciones,
            CaracteresCopiados = descripcion.Length,
            TiempoFinalizacionReal = tfReal,
        };

    [Fact]
    public void Reporte_coincide_con_la_tabla_calculada_a_mano_y_conserva_el_orden_de_terminacion()
    {
        var filas = CalculadoraTiempos.Reporte(CatalogoTerminadosEnOrden());

        Assert.Equal(
            [
                new FilaReporte("C", 2, 30, 1, Turnaround: 68, TiempoFinalizacion: 2, TiempoFinalizacionReal: 70, Espera: 38),
                new FilaReporte("B", 1, 40, 0, Turnaround: 109, TiempoFinalizacion: 4, TiempoFinalizacionReal: 110, Espera: 69),
                new FilaReporte("A", 0, 60, 0, Turnaround: 130, TiempoFinalizacion: 6, TiempoFinalizacionReal: 130, Espera: 70),
            ],
            filas);
    }

    [Fact]
    public void Fila_usa_el_TF_del_enunciado_en_la_columna_TF_y_el_real_en_el_turnaround()
    {
        var fila = CalculadoraTiempos.Fila(CatalogoTerminadosEnOrden()[1]);

        Assert.Equal(4, fila.TiempoFinalizacion);
        Assert.Equal(110, fila.TiempoFinalizacionReal);
        Assert.Equal(109, fila.Turnaround);
    }

    [Fact]
    public void Fila_con_quantum_propio_usa_ese_quantum_en_el_TF_del_enunciado()
    {
        var proceso = new ProcesoSimulado(1, new ProcesoEntrada(1, "Z", "abcdef", Prioridad.Expulsivo, QuantumPropio: 3), _configuracion)
        {
            Estado = EstadoProceso.Terminado,
            Ejecuciones = 2,
            TiempoFinalizacionReal = 80,
        };

        Assert.Equal(6, CalculadoraTiempos.Fila(proceso).TiempoFinalizacion);
    }

    [Theory]
    [InlineData(EstadoProceso.Listo)]
    [InlineData(EstadoProceso.Ejecucion)]
    public void Fila_de_un_proceso_no_terminado_lanza(EstadoProceso estado)
    {
        var proceso = Terminado(1, "A", "abc", Prioridad.Expulsivo, tl: 0, ejecuciones: 1, tfReal: 30);
        proceso.Estado = estado;

        Assert.Throws<InvalidOperationException>(() => CalculadoraTiempos.Fila(proceso));
    }

    [Fact]
    public void Fila_de_un_proceso_terminado_sin_TF_real_lanza()
    {
        var proceso = Terminado(1, "A", "abc", Prioridad.Expulsivo, tl: 0, ejecuciones: 1, tfReal: 30);
        proceso.TiempoFinalizacionReal = null;

        Assert.Throws<InvalidOperationException>(() => CalculadoraTiempos.Fila(proceso));
    }

    [Fact]
    public void Reporte_lanza_si_algun_proceso_no_termino()
    {
        var pendiente = Terminado(1, "A", "abc", Prioridad.Expulsivo, tl: 0, ejecuciones: 1, tfReal: 30);
        pendiente.Estado = EstadoProceso.Listo;

        Assert.Throws<InvalidOperationException>(() => CalculadoraTiempos.Reporte([pendiente]));
    }

    [Fact]
    public void Reporte_vacio_devuelve_lista_vacia() =>
        Assert.Empty(CalculadoraTiempos.Reporte([]));

    [Fact]
    public void SoloNoExpulsivos_deja_unicamente_las_filas_de_prioridad_no_expulsiva()
    {
        var filas = CalculadoraTiempos.Reporte(CatalogoTerminadosEnOrden());

        var soloNoExpulsivos = CalculadoraTiempos.SoloNoExpulsivos(filas);

        var fila = Assert.Single(soloNoExpulsivos);
        Assert.Equal("C", fila.Proceso);
        Assert.Equal(3, filas.Count);
    }

    [Fact]
    public void SoloNoExpulsivos_sin_ninguno_devuelve_lista_vacia()
    {
        var filas = CalculadoraTiempos.Reporte(CatalogoTerminadosEnOrden().Where(p => p.EsExpulsivo));

        Assert.Empty(CalculadoraTiempos.SoloNoExpulsivos(filas));
    }
}
