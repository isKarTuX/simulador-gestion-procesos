using SimuladorProcesos.Core.Modelos;
using SimuladorProcesos.Engine.Planificacion;
using static SimuladorProcesos.Engine.Tests.Planificacion.AuxiliarPlanificador;

namespace SimuladorProcesos.Engine.Tests.Planificacion;

public class PlanificadorInstantaneaYErroresTests
{

    // Instantánea. TH = 1 ms, quantum 2, A (exp) "abc", B (exp) "de". Cola inicial [A, B].
    //
    //   paso                         | Listos (EnEspera)        | Ejecución | Terminados (TF) | reloj
    //   0 recién creado              | [A no, B no]             | -         | []              | 0
    //   1 Despachar -> A             | [B no]                   | A         | []              | 0
    //   2 A copia "ab", vuelve       | [B no, A SI]  Espera=[A] | -         | []              | 2
    //   3 B copia "de", termina      | [A SI]        Espera=[A] | -         | [B (4)]         | 4
    //   4 A copia "c", termina       | []                       | -         | [B (4), A (5)]  | 5
    //
    // EnEspera solo es verdad para A en los pasos 2 y 3: está en Listo y ya usó la CPU una vez.
    [Fact]
    public void La_instantanea_ordena_Listos_como_la_cola_y_marca_EnEspera_a_los_que_ya_corrieron()
    {
        var planificador = Crear(1, 2, [], Proceso("A", "abc", Expulsivo), Proceso("B", "de", Expulsivo));

        var paso0 = planificador.Instantanea(EstadoSimulacion.Detenida);
        Assert.Equal(EstadoSimulacion.Detenida, paso0.Estado);
        Assert.Equal(0, paso0.RelojMs);
        Assert.Equal(["A", "B"], paso0.Listos.Select(p => p.Nombre).ToArray());
        Assert.Equal([false, false], paso0.Listos.Select(p => p.EnEspera).ToArray());
        Assert.Null(paso0.Ejecucion);
        Assert.Empty(paso0.Terminados);
        Assert.Empty(paso0.Espera);

        planificador.Despachar();
        var paso1 = planificador.Instantanea(EstadoSimulacion.EnCurso);
        Assert.Equal(EstadoSimulacion.EnCurso, paso1.Estado);
        Assert.Equal(["B"], paso1.Listos.Select(p => p.Nombre).ToArray());
        Assert.Equal("A", paso1.Ejecucion?.Nombre);
        Assert.Equal(EstadoProceso.Ejecucion, paso1.Ejecucion?.Estado);
        Assert.False(paso1.Ejecucion?.EnEspera);
        Assert.Empty(paso1.Espera);

        planificador.CopiarCaracter();
        planificador.CopiarCaracter();
        planificador.FinalizarTurno();
        var paso2 = planificador.Instantanea(EstadoSimulacion.Pausada);
        Assert.Equal(EstadoSimulacion.Pausada, paso2.Estado);
        Assert.Equal(2, paso2.RelojMs);
        Assert.Equal(["B", "A"], paso2.Listos.Select(p => p.Nombre).ToArray());
        Assert.Equal([false, true], paso2.Listos.Select(p => p.EnEspera).ToArray());
        Assert.Equal(["A"], paso2.Espera.Select(p => p.Nombre).ToArray());
        Assert.Null(paso2.Ejecucion);
        Assert.Equal(1, paso2.Listos[1].CaracteresRestantes);

        EjecutarTurno(planificador);
        var paso3 = planificador.Instantanea(EstadoSimulacion.EnCurso);
        Assert.Equal(4, paso3.RelojMs);
        Assert.Equal(["A"], paso3.Listos.Select(p => p.Nombre).ToArray());
        Assert.Equal(["A"], paso3.Espera.Select(p => p.Nombre).ToArray());
        Assert.Equal(["B"], paso3.Terminados.Select(p => p.Nombre).ToArray());
        Assert.Equal(4, paso3.Terminados[0].TiempoFinalizacion);

        EjecutarTurno(planificador);
        var paso4 = planificador.Instantanea(EstadoSimulacion.Finalizada);
        Assert.Equal(EstadoSimulacion.Finalizada, paso4.Estado);
        Assert.Equal(5, paso4.RelojMs);
        Assert.Empty(paso4.Listos);
        Assert.Empty(paso4.Espera);
        Assert.Equal(["B", "A"], paso4.Terminados.Select(p => p.Nombre).ToArray());
        Assert.Equal([4L, 5L], paso4.Terminados.Select(p => p.TiempoFinalizacion!.Value).ToArray());

        // Las fotos viejas no cambian aunque la simulación siga avanzando.
        Assert.Equal(0, paso0.RelojMs);
        Assert.Equal(2, paso0.Listos.Count);
        Assert.Equal(0, paso0.Listos[0].Ejecuciones);
        Assert.Equal(1, paso2.Listos.Count(p => p.EnEspera));
    }

    [Fact]
    public void Despachar_sin_nadie_en_Listos_devuelve_null_y_deja_la_CPU_libre()
    {
        var planificador = Crear(1, 2, [], Proceso("A", "a", Expulsivo));
        EjecutarTodo(planificador);

        Assert.Null(planificador.Despachar());
        Assert.Null(planificador.EnEjecucion);
    }

    [Fact]
    public void Despachar_con_la_CPU_ocupada_lanza()
    {
        var planificador = Crear(1, 2, [], Proceso("A", "abc", Expulsivo), Proceso("B", "d", Expulsivo));
        planificador.Despachar();

        Assert.Throws<InvalidOperationException>(planificador.Despachar);

        // Nada cambió: A sigue en la CPU y B sigue esperando.
        Assert.Equal("A", planificador.EnEjecucion?.Nombre);
        Assert.Equal(1, planificador.Listos.Cantidad);
    }

    [Fact]
    public void Copiar_un_caracter_sin_proceso_en_ejecucion_lanza()
    {
        var planificador = Crear(1, 2, [], Proceso("A", "ab", Expulsivo));
        Assert.Throws<InvalidOperationException>(() => planificador.CopiarCaracter());

        EjecutarTurno(planificador);
        Assert.Throws<InvalidOperationException>(() => planificador.CopiarCaracter());
        Assert.Equal(2, planificador.RelojMs);
    }

    [Fact]
    public void Finalizar_un_turno_sin_proceso_en_ejecucion_lanza()
    {
        var planificador = Crear(1, 2, [], Proceso("A", "ab", Expulsivo));

        Assert.Throws<InvalidOperationException>(planificador.FinalizarTurno);
    }

    // Quantum 2, TH = 3, A "abcde": el turno vale 2 caracteres; el tercero lanza y no mueve el reloj (6) ni el proceso.
    [Fact]
    public void Copiar_mas_caracteres_que_el_limite_del_turno_lanza()
    {
        var planificador = Crear(3, 2, [], Proceso("A", "abcde", Expulsivo));
        var proceso = planificador.Despachar()!;
        planificador.CopiarCaracter();
        planificador.CopiarCaracter();

        Assert.Throws<InvalidOperationException>(() => planificador.CopiarCaracter());

        Assert.Equal(6, planificador.RelojMs);
        Assert.Equal(2, proceso.CaracteresCopiados);
    }

    // Un proceso con descripción vacía tiene turno de 0 caracteres: no hay nada que copiar.
    [Fact]
    public void Copiar_en_un_proceso_con_descripcion_vacia_lanza()
    {
        var planificador = Crear(1, 2, [], Proceso("A", string.Empty, NoExpulsivo));
        planificador.Despachar();

        Assert.Throws<InvalidOperationException>(() => planificador.CopiarCaracter());
        Assert.Equal(0, planificador.RelojMs);
    }

    // Quantum 2, A "abcde": terminar el turno tras 1 de 2 caracteres lanza y el proceso sigue en la CPU para poder completarlo.
    [Fact]
    public void Finalizar_el_turno_de_un_expulsivo_antes_de_agotar_el_quantum_lanza()
    {
        var planificador = Crear(1, 2, [], Proceso("A", "abcde", Expulsivo));
        var proceso = planificador.Despachar()!;
        planificador.CopiarCaracter();

        Assert.Throws<InvalidOperationException>(planificador.FinalizarTurno);

        Assert.Same(proceso, planificador.EnEjecucion);
        Assert.Equal(EstadoProceso.Ejecucion, proceso.Estado);
        planificador.CopiarCaracter();
        planificador.FinalizarTurno();
        Assert.Equal(EstadoProceso.Listo, proceso.Estado);
    }

    // Un no expulsivo "abc" con 1 de 3 caracteres copiados no puede salir de la CPU: así nunca vuelve a Listo.
    [Fact]
    public void Un_no_expulsivo_no_puede_dejar_la_CPU_antes_de_terminar()
    {
        var planificador = Crear(1, 2, [], Proceso("A", "abc", NoExpulsivo));
        var proceso = planificador.Despachar()!;
        planificador.CopiarCaracter();

        Assert.Throws<InvalidOperationException>(planificador.FinalizarTurno);

        Assert.Same(proceso, planificador.EnEjecucion);
        Assert.True(planificador.Listos.EstaVacia);
    }

    [Fact]
    public void El_constructor_rechaza_argumentos_nulos()
    {
        var procesos = new[] { Proceso("A", "a", Expulsivo) };
        var configuracion = new ConfiguracionSimulacion(1, 2);

        Assert.Throws<ArgumentNullException>(() => new PlanificadorRoundRobin(null!, configuracion, _ => { }));
        Assert.Throws<ArgumentNullException>(() => new PlanificadorRoundRobin(procesos, null!, _ => { }));
        Assert.Throws<ArgumentNullException>(() => new PlanificadorRoundRobin(procesos, configuracion, null!));
    }

    // Un quantum propio de 0 o negativo haría turnos sin progreso (bucle infinito), así que se rechaza al cargar.
    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void Un_quantum_propio_no_positivo_se_rechaza(int quantumPropio) =>
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Crear(1, 2, [], Proceso("A", "abc", Expulsivo, quantumPropio)));
}
