using SimuladorProcesos.Core.Modelos;
using SimuladorProcesos.Engine.Actividad;
using SimuladorProcesos.Engine.Tests.Planificacion;
using static SimuladorProcesos.Engine.Tests.Concurrencia.AuxiliarSimulador;

namespace SimuladorProcesos.Engine.Tests.Concurrencia;

/// <summary>Ciclo de vida de ISimulador: qué llamadas son válidas en cada EstadoSimulacion y qué ve la UI antes de correr.</summary>
[Trait("Category", "Concurrencia")]
public class SimuladorTransicionesTests
{
    private static readonly ConfiguracionSimulacion _configuracion = new(ThMs, 3);

    private static SimuladorRoundRobin NuevoSimulador() => new(new DestinoEnMemoria());

    // Un proceso largo mantiene la simulación en curso el tiempo suficiente para probar llamadas inválidas.
    private static SimuladorRoundRobin EnCurso()
    {
        var simulador = Cargado(new DestinoEnMemoria(), [AuxiliarPlanificador.Proceso("Largo", Texto(2000), Prioridad.Expulsivo)], 50, thMs: 2);
        simulador.Iniciar();
        return simulador;
    }

    [Fact]
    public void Antes_de_cargar_la_instantanea_y_el_reporte_estan_vacios()
    {
        using var simulador = NuevoSimulador();

        var foto = simulador.ObtenerInstantanea();

        Assert.Equal(EstadoSimulacion.Detenida, simulador.Estado);
        Assert.Equal(EstadoSimulacion.Detenida, foto.Estado);
        Assert.Equal(0, foto.RelojMs);
        Assert.Empty(foto.Listos);
        Assert.Null(foto.Ejecucion);
        Assert.Empty(foto.Terminados);
        Assert.Empty(simulador.ObtenerReporte());
    }

    [Fact]
    public void Cargar_deja_a_todos_en_Listo_sin_crear_hilos_y_avisa_cada_llegada()
    {
        var (procesos, quantum) = Catalogo("mezcla");
        using var simulador = NuevoSimulador();
        var llegadas = new List<string>();
        simulador.CambioEstado += (_, e) => llegadas.Add(AuxiliarPlanificador.Formato(e));

        simulador.Cargar(procesos, new ConfiguracionSimulacion(ThMs, quantum));

        var foto = simulador.ObtenerInstantanea();
        Assert.Equal(EstadoSimulacion.Detenida, foto.Estado);
        Assert.Equal(procesos.Select(p => p.Nombre), foto.Listos.Select(p => p.Nombre));
        Assert.Equal(procesos.Select(p => $"{p.Nombre}: nuevo > Listo @0"), llegadas);
        Assert.Empty(simulador.Hilos);
    }

    [Fact]
    public void Iniciar_sin_cargar_lanza()
    {
        using var simulador = NuevoSimulador();

        Assert.Throws<InvalidOperationException>(simulador.Iniciar);
    }

    [Fact]
    public void Reiniciar_sin_cargar_lanza()
    {
        using var simulador = NuevoSimulador();

        Assert.Throws<InvalidOperationException>(simulador.Reiniciar);
    }

    [Fact]
    public void Pausar_y_Reanudar_estando_Detenida_lanzan()
    {
        var (procesos, quantum) = Catalogo("uno solo");
        using var simulador = Cargado(new DestinoEnMemoria(), procesos, quantum);

        Assert.Throws<InvalidOperationException>(simulador.Pausar);
        Assert.Throws<InvalidOperationException>(simulador.Reanudar);
        Assert.Equal(EstadoSimulacion.Detenida, simulador.Estado);
    }

    [Fact]
    public void Iniciar_dos_veces_lanza_y_no_crea_hilos_de_mas()
    {
        using var simulador = EnCurso();
        var hilos = simulador.Hilos;

        Assert.Throws<InvalidOperationException>(simulador.Iniciar);

        Assert.Equal(hilos, simulador.Hilos);

        // Hilos de fondo: si la aplicación se cierra sin Dispose, no la mantienen viva.
        Assert.All(hilos, hilo => Assert.True(hilo.IsBackground));
    }

    [Fact]
    public void Cargar_con_la_simulacion_en_curso_o_pausada_lanza()
    {
        using var simulador = EnCurso();
        var (procesos, quantum) = Catalogo("uno solo");

        Assert.Throws<InvalidOperationException>(() => simulador.Cargar(procesos, new ConfiguracionSimulacion(ThMs, quantum)));

        simulador.Pausar();
        Assert.Throws<InvalidOperationException>(() => simulador.Cargar(procesos, new ConfiguracionSimulacion(ThMs, quantum)));
    }

    [Fact]
    public void Pausar_y_Reanudar_son_idempotentes()
    {
        using var simulador = EnCurso();

        simulador.Reanudar();
        Assert.Equal(EstadoSimulacion.EnCurso, simulador.Estado);

        simulador.Pausar();
        simulador.Pausar();
        Assert.Equal(EstadoSimulacion.Pausada, simulador.Estado);

        simulador.Reanudar();
        simulador.Reanudar();
        Assert.Equal(EstadoSimulacion.EnCurso, simulador.Estado);
    }

    [Fact]
    public void Pausar_y_Reanudar_estando_Finalizada_lanzan()
    {
        var (procesos, quantum) = Catalogo("uno solo");
        using var simulador = Cargado(new DestinoEnMemoria(), procesos, quantum);
        EjecutarHastaElFinal(simulador);

        Assert.Throws<InvalidOperationException>(simulador.Pausar);
        Assert.Throws<InvalidOperationException>(simulador.Reanudar);
        Assert.Throws<InvalidOperationException>(simulador.Iniciar);
        Assert.Equal(EstadoSimulacion.Finalizada, simulador.Estado);
    }

    [Fact]
    public void Se_puede_cargar_otro_catalogo_tras_Finalizada()
    {
        var destino = new DestinoEnMemoria();
        var (primeros, q1) = Catalogo("expulsivos");
        var (segundos, q2) = Catalogo("no expulsivos");
        var (reporteEsperado, _) = ReferenciaSinHilos(segundos, q2);
        using var simulador = Cargado(destino, primeros, q1);
        EjecutarHastaElFinal(simulador);

        simulador.Cargar(segundos, new ConfiguracionSimulacion(ThMs, q2));

        Assert.Equal(EstadoSimulacion.Detenida, simulador.Estado);
        Assert.Empty(simulador.ObtenerReporte());
        EjecutarHastaElFinal(simulador);
        Assert.Equal(reporteEsperado, simulador.ObtenerReporte());
    }

    [Fact]
    public void Reiniciar_tras_Finalizada_deja_los_mismos_procesos_listos_para_otra_corrida()
    {
        var (procesos, quantum) = Catalogo("mezcla");
        var (reporteEsperado, _) = ReferenciaSinHilos(procesos, quantum);
        using var simulador = Cargado(new DestinoEnMemoria(), procesos, quantum);
        EjecutarHastaElFinal(simulador);

        simulador.Reiniciar();

        Assert.Equal(EstadoSimulacion.Detenida, simulador.Estado);
        Assert.Equal(procesos.Length, simulador.ObtenerInstantanea().Listos.Count);
        EjecutarHastaElFinal(simulador);
        Assert.Equal(reporteEsperado, simulador.ObtenerReporte());
    }

    [Fact]
    public void Un_quantum_propio_invalido_lanza_y_conserva_lo_cargado_antes()
    {
        var (procesos, quantum) = Catalogo("expulsivos");
        using var simulador = Cargado(new DestinoEnMemoria(), procesos, quantum);
        var malos = new[] { AuxiliarPlanificador.Proceso("Mal", "abc", Prioridad.Expulsivo, quantumPropio: 0) };

        Assert.Throws<ArgumentOutOfRangeException>(() => simulador.Cargar(malos, _configuracion));

        Assert.Equal(procesos.Select(p => p.Nombre), simulador.ObtenerInstantanea().Listos.Select(p => p.Nombre));
        EjecutarHastaElFinal(simulador);
        Assert.Equal(procesos.Length, simulador.ObtenerReporte().Count);
    }

    [Fact]
    public void Cargar_rechaza_argumentos_nulos()
    {
        using var simulador = NuevoSimulador();

        Assert.Throws<ArgumentNullException>(() => simulador.Cargar(null!, _configuracion));
        Assert.Throws<ArgumentNullException>(() => simulador.Cargar([], null!));
    }

    [Fact]
    public void Una_lista_vacia_termina_de_inmediato_con_reporte_vacio()
    {
        using var simulador = Cargado(new DestinoEnMemoria(), [], 3);

        EjecutarHastaElFinal(simulador);

        Assert.Equal(EstadoSimulacion.Finalizada, simulador.Estado);
        Assert.Empty(simulador.ObtenerReporte());
        Assert.Equal(0, simulador.ObtenerInstantanea().RelojMs);
    }

    [Fact]
    public void La_lista_cargada_se_copia_y_modificarla_despues_no_cambia_la_simulacion()
    {
        var lista = new List<ProcesoEntrada>
        {
            AuxiliarPlanificador.Proceso("Ana", "abc", Prioridad.Expulsivo),
            AuxiliarPlanificador.Proceso("Beto", "de", Prioridad.Expulsivo),
        };
        using var simulador = Cargado(new DestinoEnMemoria(), lista, 3);
        lista.Clear();

        simulador.Reiniciar();
        EjecutarHastaElFinal(simulador);

        Assert.Equal(["Ana", "Beto"], simulador.ObtenerReporte().Select(f => f.Proceso));
    }

    [Fact]
    public void Despues_de_Dispose_las_operaciones_lanzan_ObjectDisposedException_y_Dispose_es_repetible()
    {
        var (procesos, quantum) = Catalogo("uno solo");
        var simulador = Cargado(new DestinoEnMemoria(), procesos, quantum);

        simulador.Dispose();
        simulador.Dispose();

        Assert.Throws<ObjectDisposedException>(simulador.Iniciar);
        Assert.Throws<ObjectDisposedException>(simulador.Pausar);
        Assert.Throws<ObjectDisposedException>(simulador.Reanudar);
        Assert.Throws<ObjectDisposedException>(simulador.Reiniciar);
        Assert.Throws<ObjectDisposedException>(() => simulador.Cargar(procesos, _configuracion));
        Assert.Throws<ObjectDisposedException>(() => simulador.ObtenerInstantanea());
        Assert.Throws<ObjectDisposedException>(() => simulador.ObtenerReporte());
    }

    [Fact]
    public void Dispose_sin_haber_iniciado_no_hace_nada_peligroso()
    {
        var simulador = new SimuladorRoundRobin();

        simulador.Dispose();

        Assert.Empty(simulador.Hilos);
    }

    [Fact]
    public void Un_proceso_sin_descripcion_pasa_por_Ejecucion_y_termina_sin_escribir()
    {
        var destino = new DestinoEnMemoria();
        var procesos = new[]
        {
            AuxiliarPlanificador.Proceso("Vacio", string.Empty, Prioridad.Expulsivo),
            AuxiliarPlanificador.Proceso("Lleno", "ab", Prioridad.Expulsivo),
        };
        var traza = new List<string>();
        using var simulador = new SimuladorRoundRobin(destino);
        simulador.CambioEstado += (_, e) => traza.Add(AuxiliarPlanificador.Formato(e));
        simulador.Cargar(procesos, new ConfiguracionSimulacion(ThMs, 3));

        EjecutarHastaElFinal(simulador);

        Assert.Equal(
            [
                "Vacio: nuevo > Listo @0",
                "Lleno: nuevo > Listo @0",
                "Vacio: Listo > Ejecucion @0",
                "Vacio: Ejecucion > Terminado @0",
                "Lleno: Listo > Ejecucion @0",
                "Lleno: Ejecucion > Terminado @2",
            ],
            traza);
        Assert.Equal(string.Empty, destino.Texto(procesos[0].Pid));
        Assert.Equal("ab", destino.Texto(procesos[1].Pid));
    }
}
