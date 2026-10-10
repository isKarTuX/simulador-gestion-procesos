using SimuladorProcesos.Core.Modelos;
using SimuladorProcesos.Engine.Planificacion;
using static SimuladorProcesos.Engine.Tests.Planificacion.AuxiliarPlanificador;

namespace SimuladorProcesos.Engine.Tests.Planificacion;

/// <summary>
/// Casos de CONTEXT §6-M8 sobre la lógica sin hilos. Cada traza se calculó a mano en el comentario;
/// los valores esperados están escritos como literales, nunca calculados con el propio planificador.
/// </summary>
public class PlanificadorRoundRobinTests
{

    // Todos expulsivos. TH = 10 ms, quantum global = 2. A = "abcde" (5), B = "fg" (2), C = "hijklm" (6).
    // Total 13 caracteres, reloj final 13 x 10 = 130.
    //
    //   turno | sale de Listos | copia | restan | reloj | consecuencia           | Listos después
    //   1     | A              | ab    | 3      | 20    | vuelve a Listo          | [B, C, A]
    //   2     | B              | fg    | 0      | 40    | Terminado (TF real 40)  | [C, A]
    //   3     | C              | hi    | 4      | 60    | vuelve a Listo          | [A, C]
    //   4     | A              | cd    | 1      | 80    | vuelve a Listo          | [C, A]
    //   5     | C              | jk    | 2      | 100   | vuelve a Listo          | [A, C]
    //   6     | A              | e     | 0      | 110   | Terminado (TF real 110) | [C]
    //   7     | C              | lm    | 0      | 130   | Terminado (TF real 130) | []
    //
    // Ejecuciones: A = 3, B = 1, C = 3. Orden de turnos A, B, C, A, C, A, C.
    [Fact]
    public void Todos_expulsivos_se_turnan_por_quantum_y_vuelven_al_final_de_Listos()
    {
        var registro = new List<string>();
        var planificador = Crear(
            10, 2, registro,
            Proceso("A", "abcde", Expulsivo),
            Proceso("B", "fg", Expulsivo),
            Proceso("C", "hijklm", Expulsivo));

        var turnos = EjecutarTodo(planificador);

        Assert.Equal(["A:ab", "B:fg", "C:hi", "A:cd", "C:jk", "A:e", "C:lm"], turnos);
        Assert.Equal(["B@40", "A@110", "C@130"], Finalizaciones(planificador));
        Assert.Equal(130, planificador.RelojMs);
        Assert.Equal(
            [
                "A: nuevo > Listo @0",
                "B: nuevo > Listo @0",
                "C: nuevo > Listo @0",
                "A: Listo > Ejecucion @0",
                "A: Ejecucion > Listo @20",
                "B: Listo > Ejecucion @20",
                "B: Ejecucion > Terminado @40",
                "C: Listo > Ejecucion @40",
                "C: Ejecucion > Listo @60",
                "A: Listo > Ejecucion @60",
                "A: Ejecucion > Listo @80",
                "C: Listo > Ejecucion @80",
                "C: Ejecucion > Listo @100",
                "A: Listo > Ejecucion @100",
                "A: Ejecucion > Terminado @110",
                "C: Listo > Ejecucion @110",
                "C: Ejecucion > Terminado @130",
            ],
            registro);
    }

    // Misma traza anterior, solo para fijar las Ejecuciones de cada proceso (A = 3, B = 1, C = 3).
    [Fact]
    public void Todos_expulsivos_cuentan_una_ejecucion_por_cada_turno()
    {
        var planificador = Crear(
            10, 2, [],
            Proceso("A", "abcde", Expulsivo),
            Proceso("B", "fg", Expulsivo),
            Proceso("C", "hijklm", Expulsivo));

        EjecutarTodo(planificador);

        var ejecuciones = planificador.Terminados.ToDictionary(p => p.Nombre, p => p.Ejecuciones);
        Assert.Equal(3, ejecuciones["A"]);
        Assert.Equal(1, ejecuciones["B"]);
        Assert.Equal(3, ejecuciones["C"]);
    }

    // Todos no expulsivos. TH = 5 ms, quantum 2 (no les afecta). A = "abc" (3), B = "de" (2), C = "fghi" (4).
    // Cada uno corre hasta terminar, en orden de llegada, y nunca vuelve a Listos.
    //
    //   turno | proceso | copia | reloj        | Ejecuciones
    //   1     | A       | abc   | 3 x 5 = 15   | 1
    //   2     | B       | de    | 15 + 10 = 25 | 1
    //   3     | C       | fghi  | 25 + 20 = 45 | 1
    //
    // Total 9 caracteres x 5 = 45.
    [Fact]
    public void Todos_no_expulsivos_corren_hasta_terminar_en_orden_de_llegada()
    {
        var registro = new List<string>();
        var planificador = Crear(
            5, 2, registro,
            Proceso("A", "abc", NoExpulsivo),
            Proceso("B", "de", NoExpulsivo),
            Proceso("C", "fghi", NoExpulsivo));

        var turnos = EjecutarTodo(planificador);

        Assert.Equal(["A:abc", "B:de", "C:fghi"], turnos);
        Assert.Equal(["A@15", "B@25", "C@45"], Finalizaciones(planificador));
        Assert.All(planificador.Terminados, p => Assert.Equal(1, p.Ejecuciones));
        Assert.DoesNotContain(registro, linea => linea.Contains("Ejecucion > Listo"));
        Assert.Equal(45, planificador.RelojMs);
    }

    // Mezcla. TH = 1 ms, quantum 2. A (exp) "abcde" (5), B (no exp) "fghi" (4), C (exp) "jk" (2), D (no exp) "lmn" (3).
    // Total 14 caracteres, reloj final 14.
    //
    //   turno | proceso | copia | restan | reloj | consecuencia | Listos después
    //   1     | A       | ab    | 3      | 2     | vuelve a Listo | [B, C, D, A]
    //   2     | B       | fghi  | 0      | 6     | Terminado      | [C, D, A]   (B ignora el quantum)
    //   3     | C       | jk    | 0      | 8     | Terminado      | [D, A]      (justo un quantum)
    //   4     | D       | lmn   | 0      | 11    | Terminado      | [A]
    //   5     | A       | cd    | 1      | 13    | vuelve a Listo | [A]         (sola en la cola)
    //   6     | A       | e     | 0      | 14    | Terminado      | []
    [Fact]
    public void Mezcla_de_expulsivos_y_no_expulsivos_respeta_cada_regla()
    {
        var registro = new List<string>();
        var planificador = Crear(
            1, 2, registro,
            Proceso("A", "abcde", Expulsivo),
            Proceso("B", "fghi", NoExpulsivo),
            Proceso("C", "jk", Expulsivo),
            Proceso("D", "lmn", NoExpulsivo));

        var turnos = EjecutarTodo(planificador);

        Assert.Equal(["A:ab", "B:fghi", "C:jk", "D:lmn", "A:cd", "A:e"], turnos);
        Assert.Equal(["B@6", "C@8", "D@11", "A@14"], Finalizaciones(planificador));
        Assert.Equal(14, planificador.RelojMs);
        Assert.DoesNotContain(registro, linea => linea.StartsWith("B:") && linea.Contains("Ejecucion > Listo"));
        Assert.DoesNotContain(registro, linea => linea.StartsWith("D:") && linea.Contains("Ejecucion > Listo"));
        Assert.Equal(2, registro.Count(linea => linea.StartsWith("A:") && linea.Contains("Ejecucion > Listo")));
    }

    // Un solo proceso expulsivo. TH = 3 ms, quantum 2, A = "abcde" (5). Siempre es el único en Listos.
    //
    //   turno | copia | restan | reloj   | consecuencia
    //   1     | ab    | 3      | 6       | Listo, y es el único en la cola
    //   2     | cd    | 1      | 12      | Listo
    //   3     | e     | 0      | 15      | Terminado (TF real 15)
    [Fact]
    public void Un_solo_proceso_expulsivo_se_redespacha_hasta_terminar()
    {
        var registro = new List<string>();
        var planificador = Crear(3, 2, registro, Proceso("A", "abcde", Expulsivo));

        var turnos = EjecutarTodo(planificador);

        Assert.Equal(["A:ab", "A:cd", "A:e"], turnos);
        Assert.Equal(["A@15"], Finalizaciones(planificador));
        Assert.Equal(
            [
                "A: nuevo > Listo @0",
                "A: Listo > Ejecucion @0",
                "A: Ejecucion > Listo @6",
                "A: Listo > Ejecucion @6",
                "A: Ejecucion > Listo @12",
                "A: Listo > Ejecucion @12",
                "A: Ejecucion > Terminado @15",
            ],
            registro);
    }

    // Un solo proceso no expulsivo. TH = 3 ms, A = "abcde" (5): un único turno de 5 caracteres, termina en 15.
    [Fact]
    public void Un_solo_proceso_no_expulsivo_termina_en_un_turno()
    {
        var registro = new List<string>();
        var planificador = Crear(3, 2, registro, Proceso("A", "abcde", NoExpulsivo));

        var turnos = EjecutarTodo(planificador);

        Assert.Equal(["A:abcde"], turnos);
        Assert.Equal(
            ["A: nuevo > Listo @0", "A: Listo > Ejecucion @0", "A: Ejecucion > Terminado @15"],
            registro);
    }

    // Descripción vacía. TH = 10 ms, quantum 5. A (exp) "ab", B (no exp) "", C (exp) "c".
    //
    //   turno | proceso | copia | reloj | consecuencia
    //   1     | A       | ab    | 20    | Terminado (cabe en el quantum)
    //   2     | B       | (nada)| 20    | se despacha (Ejecuciones = 1) y termina sin que avance el reloj
    //   3     | C       | c     | 30    | Terminado
    [Fact]
    public void Descripcion_vacia_termina_en_su_turno_sin_copiar_caracteres()
    {
        var registro = new List<string>();
        var planificador = Crear(
            10, 5, registro,
            Proceso("A", "ab", Expulsivo),
            Proceso("B", string.Empty, NoExpulsivo),
            Proceso("C", "c", Expulsivo));

        var turnos = EjecutarTodo(planificador);

        Assert.Equal(["A:ab", "B:", "C:c"], turnos);
        Assert.Equal(["A@20", "B@20", "C@30"], Finalizaciones(planificador));
        Assert.Contains("B: Listo > Ejecucion @20", registro);
        Assert.Contains("B: Ejecucion > Terminado @20", registro);
        var vacio = planificador.Terminados.Single(p => p.Nombre == "B");
        Assert.Equal(1, vacio.Ejecuciones);
        Assert.Equal(0, vacio.Rafaga);
    }

    // Un expulsivo con descripción vacía tampoco vuelve a Listo: TH = 4, A (exp) "", reloj final 0.
    [Fact]
    public void Descripcion_vacia_en_un_expulsivo_pasa_directo_a_Terminado()
    {
        var registro = new List<string>();
        var planificador = Crear(4, 3, registro, Proceso("A", string.Empty, Expulsivo));

        var turnos = EjecutarTodo(planificador);

        Assert.Equal(["A:"], turnos);
        Assert.Equal(["A@0"], Finalizaciones(planificador));
        Assert.Equal(0, planificador.RelojMs);
        Assert.Equal(
            ["A: nuevo > Listo @0", "A: Listo > Ejecucion @0", "A: Ejecucion > Terminado @0"],
            registro);
    }

    // Quantum propio. TH = 1 ms, quantum global 2. A (exp, quantum propio 4) "abcdef", B (exp, global 2) "ghijkl".
    //
    //   turno | proceso | copia | restan | reloj | consecuencia  | Listos después
    //   1     | A       | abcd  | 2      | 4     | vuelve a Listo | [B, A]
    //   2     | B       | gh    | 4      | 6     | vuelve a Listo | [A, B]
    //   3     | A       | ef    | 0      | 8     | Terminado      | [B]   (min(4, 2 restantes) = 2)
    //   4     | B       | ij    | 2      | 10    | vuelve a Listo | [B]
    //   5     | B       | kl    | 0      | 12    | Terminado      | []
    //
    // Ejecuciones: A = 2, B = 3. Total 12 caracteres x 1 = 12.
    [Fact]
    public void El_quantum_propio_reemplaza_al_global_solo_en_su_proceso()
    {
        var planificador = Crear(
            1, 2, [],
            Proceso("A", "abcdef", Expulsivo, quantumPropio: 4),
            Proceso("B", "ghijkl", Expulsivo));

        var turnos = EjecutarTodo(planificador);

        Assert.Equal(["A:abcd", "B:gh", "A:ef", "B:ij", "B:kl"], turnos);
        Assert.Equal(["A@8", "B@12"], Finalizaciones(planificador));
        var ejecuciones = planificador.Terminados.ToDictionary(p => p.Nombre, p => p.Ejecuciones);
        Assert.Equal(2, ejecuciones["A"]);
        Assert.Equal(3, ejecuciones["B"]);
    }

    // Tiempos de llegada: el primero entra con Listos vacía (TL = 0) y cada uno siguiente es TL del último + 1.
    //   A -> 0, B -> 0 + 1 = 1, C -> 1 + 1 = 2, D -> 2 + 1 = 3. Numeración "Procesos N": 1, 2, 3, 4.
    [Fact]
    public void Los_tiempos_de_llegada_son_0_1_2_3_y_los_numeros_van_de_1_a_N()
    {
        var planificador = Crear(
            1, 2, [],
            Proceso("A", "a", Expulsivo),
            Proceso("B", "b", NoExpulsivo),
            Proceso("C", "c", Expulsivo),
            Proceso("D", "d", NoExpulsivo));

        Assert.Equal([0L, 1L, 2L, 3L], planificador.Listos.Select(p => p.TiempoLlegada).ToArray());
        Assert.Equal([1, 2, 3, 4], planificador.Listos.Select(p => p.Numero).ToArray());
        Assert.Equal(["A", "B", "C", "D"], planificador.Listos.Select(p => p.Nombre).ToArray());
    }

    // Un solo proceso: Listos estaba vacía, TL = 0.
    [Fact]
    public void Un_solo_proceso_llega_en_el_tiempo_cero() =>
        Assert.Equal(0, Crear(1, 2, [], Proceso("A", "a", Expulsivo)).Listos.VerFrente().TiempoLlegada);

    // Al construir todos quedan en Listo y el reloj en 0: nadie ha usado la CPU.
    [Fact]
    public void Al_crear_todos_estan_en_Listo_con_el_reloj_en_cero()
    {
        var planificador = Crear(1, 2, [], Proceso("A", "a", Expulsivo), Proceso("B", "b", Expulsivo));

        Assert.Equal(0, planificador.RelojMs);
        Assert.Equal(2, planificador.Listos.Cantidad);
        Assert.Null(planificador.EnEjecucion);
        Assert.True(planificador.Terminados.EstaVacia);
        Assert.False(planificador.TodosTerminados);
        Assert.All(planificador.Listos, p => Assert.Equal(EstadoProceso.Listo, p.Estado));
    }

    // Sin procesos: no hay nada que despachar y se considera terminado de inmediato.
    [Fact]
    public void Sin_procesos_no_hay_nada_que_despachar()
    {
        var planificador = Crear(1, 2, []);

        Assert.True(planificador.TodosTerminados);
        Assert.Null(planificador.Despachar());
    }

    // Reloj. TH = 7 ms, quantum 2. A (exp) "abc", B (no exp) "defgh", C (exp) "ijkl": 12 caracteres, 12 x 7 = 84.
    //
    //   turno | proceso | copia  | reloj
    //   1     | A       | ab     | 14
    //   2     | B       | defgh  | 14 + 35 = 49   (Terminado)
    //   3     | C       | ij     | 63
    //   4     | A       | c      | 70             (Terminado)
    //   5     | C       | kl     | 84             (Terminado)
    [Fact]
    public void El_reloj_final_es_TH_por_el_total_de_caracteres()
    {
        var planificador = Crear(
            7, 2, [],
            Proceso("A", "abc", Expulsivo),
            Proceso("B", "defgh", NoExpulsivo),
            Proceso("C", "ijkl", Expulsivo));

        var turnos = EjecutarTodo(planificador);

        Assert.Equal(["A:ab", "B:defgh", "C:ij", "A:c", "C:kl"], turnos);
        Assert.Equal(["B@49", "A@70", "C@84"], Finalizaciones(planificador));
        Assert.Equal(84, planificador.RelojMs);
    }

    // Cada carácter copiado suma exactamente TH: TH = 7, el reloj pasa de 0 a 7 y a 14.
    [Fact]
    public void Cada_caracter_copiado_avanza_el_reloj_TH_milisegundos()
    {
        var planificador = Crear(7, 5, [], Proceso("A", "xyz", Expulsivo));
        planificador.Despachar();

        Assert.Equal('x', planificador.CopiarCaracter());
        Assert.Equal(7, planificador.RelojMs);
        Assert.Equal('y', planificador.CopiarCaracter());
        Assert.Equal(14, planificador.RelojMs);
    }

    // Límite del turno con quantum 4: expulsivo = min(4, restantes); no expulsivo = todo lo que falta.
    [Theory]
    [InlineData(Prioridad.Expulsivo, 10, 4)]
    [InlineData(Prioridad.Expulsivo, 3, 3)]
    [InlineData(Prioridad.Expulsivo, 4, 4)]
    [InlineData(Prioridad.Expulsivo, 0, 0)]
    [InlineData(Prioridad.NoExpulsivo, 10, 10)]
    [InlineData(Prioridad.NoExpulsivo, 0, 0)]
    public void El_limite_del_turno_depende_de_la_prioridad_y_de_lo_que_falta(Prioridad prioridad, int largo, int esperado)
    {
        var planificador = Crear(1, 4, [], Proceso("A", new string('x', largo), prioridad));

        Assert.Equal(esperado, planificador.LimiteDelTurno(planificador.Listos.VerFrente()));
    }

    // El límite se recalcula con lo que falta: A (exp, quantum 4) con 6 caracteres copia 4 y le quedan 2, así que su segundo turno es 2.
    [Fact]
    public void El_limite_del_segundo_turno_usa_los_caracteres_que_quedan()
    {
        var planificador = Crear(1, 4, [], Proceso("A", "abcdef", Expulsivo));
        var proceso = planificador.Listos.VerFrente();
        Assert.Equal(4, planificador.LimiteDelTurno(proceso));

        EjecutarTurno(planificador);

        Assert.Equal(2, planificador.LimiteDelTurno(proceso));
    }

    // Quien escucha el evento ya ve el estado nuevo. Quantum 1, TH = 1, A (exp) "ab", B (no exp) "c", cola [A, B].
    //   Despachar A       -> CPU = A, en Listos queda B (1)
    //   copia 'a' (reloj 1) y A vuelve a Listo -> CPU libre, Listos = [B, A] (2)
    //   Despachar B       -> CPU = B, en Listos queda A (1)
    //   copia 'c' (reloj 2) y B termina -> CPU libre, en Listos sigue A (1)
    [Fact]
    public void El_aviso_llega_despues_de_actualizar_el_estado_del_planificador()
    {
        var registro = new List<string>();
        PlanificadorRoundRobin? planificador = null;
        planificador = new PlanificadorRoundRobin(
            [Proceso("A", "ab", Expulsivo), Proceso("B", "c", NoExpulsivo)],
            new ConfiguracionSimulacion(1, 1),
            e => registro.Add($"{Formato(e)} | CPU={planificador?.EnEjecucion?.Nombre ?? "libre"} | listos={planificador?.Listos.Cantidad}"));
        registro.Clear();

        EjecutarTurno(planificador);
        EjecutarTurno(planificador);

        Assert.Equal(
            [
                "A: Listo > Ejecucion @0 | CPU=A | listos=1",
                "A: Ejecucion > Listo @1 | CPU=libre | listos=2",
                "B: Listo > Ejecucion @1 | CPU=B | listos=1",
                "B: Ejecucion > Terminado @2 | CPU=libre | listos=1",
            ],
            registro);
    }

    // Contenido del evento. TH = 5, quantum 2, A (exp) "abc" sola.
    //   llegada       : Listo,      Ejecuciones 0, TL 0, reloj 0
    //   despacho      : Ejecucion,  Ejecuciones 1, restan 3, reloj 0
    //   fin del turno1: Listo,      Ejecuciones 1, restan 1, EnEspera, reloj 10
    //   fin del turno2: Terminado,  restan 0, TF 15, ráfaga 3 x 5 = 15, reloj 15
    [Fact]
    public void Los_eventos_llevan_la_instantanea_del_proceso_en_ese_momento()
    {
        var eventos = new List<CambioEstadoEventArgs>();
        var planificador = new PlanificadorRoundRobin(
            [Proceso("A", "abc", Expulsivo)], new ConfiguracionSimulacion(5, 2), eventos.Add);

        EjecutarTodo(planificador);

        Assert.Equal(5, eventos.Count);

        Assert.Null(eventos[0].Desde);
        Assert.Equal(EstadoProceso.Listo, eventos[0].Hacia);
        Assert.Equal(EstadoProceso.Listo, eventos[0].Proceso.Estado);
        Assert.Equal(0, eventos[0].Proceso.Ejecuciones);
        Assert.Equal(0, eventos[0].Proceso.TiempoLlegada);
        Assert.Equal(0, eventos[0].RelojMs);

        Assert.Equal(EstadoProceso.Listo, eventos[1].Desde);
        Assert.Equal(EstadoProceso.Ejecucion, eventos[1].Hacia);
        Assert.Equal(EstadoProceso.Ejecucion, eventos[1].Proceso.Estado);
        Assert.Equal(1, eventos[1].Proceso.Ejecuciones);
        Assert.Equal(3, eventos[1].Proceso.CaracteresRestantes);
        Assert.Equal(0, eventos[1].RelojMs);

        Assert.Equal(EstadoProceso.Ejecucion, eventos[2].Desde);
        Assert.Equal(EstadoProceso.Listo, eventos[2].Hacia);
        Assert.Equal(1, eventos[2].Proceso.Ejecuciones);
        Assert.Equal(1, eventos[2].Proceso.CaracteresRestantes);
        Assert.True(eventos[2].Proceso.EnEspera);
        Assert.Equal(10, eventos[2].RelojMs);

        Assert.Equal(EstadoProceso.Terminado, eventos[4].Hacia);
        Assert.Equal(EstadoProceso.Terminado, eventos[4].Proceso.Estado);
        Assert.Equal(0, eventos[4].Proceso.CaracteresRestantes);
        Assert.Equal(15, eventos[4].Proceso.TiempoFinalizacion);
        Assert.Equal(15, eventos[4].Proceso.Rafaga);
        Assert.Equal(15, eventos[4].RelojMs);
    }

    public static IEnumerable<object[]> Semillas() => Enumerable.Range(0, 50).Select(semilla => new object[] { semilla });

    // Invariantes sobre 50 catálogos pseudoaleatorios (semilla fija, por eso repetibles). Los esperados salen de fórmulas
    // cerradas, no del planificador: reloj final = TH x total de caracteres; un no expulsivo (o uno vacío) usa la CPU 1 vez;
    // un expulsivo con L caracteres y quantum Q la usa techo(L / Q) veces; cada descripción se copia completa y en orden.
    [Theory]
    [MemberData(nameof(Semillas))]
    public void Catalogos_aleatorios_cumplen_las_invariantes_de_Round_Robin(int semilla)
    {
        var azar = new Random(semilla);
        var th = azar.Next(1, 20);
        var quantum = azar.Next(1, 6);
        var entradas = Enumerable.Range(0, azar.Next(1, 8))
            .Select(i => Proceso(
                $"P{i}",
                new string(Enumerable.Range(0, azar.Next(0, 12)).Select(j => (char)('a' + ((i + j) % 26))).ToArray()),
                azar.Next(2) == 0 ? Expulsivo : NoExpulsivo,
                azar.Next(3) == 0 ? azar.Next(1, 6) : null))
            .ToArray();
        var registro = new List<string>();
        var planificador = Crear(th, quantum, registro, entradas);

        var turnos = EjecutarTodo(planificador);

        Assert.Equal(entradas.Length, planificador.Terminados.Cantidad);
        Assert.True(planificador.Listos.EstaVacia);
        Assert.Null(planificador.EnEjecucion);
        Assert.Equal((long)th * entradas.Sum(e => e.Descripcion.Length), planificador.RelojMs);

        foreach (var entrada in entradas)
        {
            var proceso = planificador.Terminados.Single(p => p.Nombre == entrada.Nombre);
            var largo = entrada.Descripcion.Length;
            var quantumEfectivo = entrada.QuantumPropio ?? quantum;
            var esperadas = entrada.Prioridad == NoExpulsivo || largo == 0 ? 1 : (largo + quantumEfectivo - 1) / quantumEfectivo;
            Assert.Equal(esperadas, proceso.Ejecuciones);
            Assert.Equal(0, proceso.CaracteresRestantes);

            var copiado = string.Concat(turnos.Where(t => t.StartsWith(entrada.Nombre + ":")).Select(t => t[(entrada.Nombre.Length + 1)..]));
            Assert.Equal(entrada.Descripcion, copiado);

            if (entrada.Prioridad == NoExpulsivo)
            {
                Assert.DoesNotContain(registro, linea => linea.StartsWith(entrada.Nombre + ":") && linea.Contains("Ejecucion > Listo"));
            }
        }

        var tiempos = planificador.Terminados.Select(p => p.TiempoFinalizacionReal!.Value).ToArray();
        Assert.Equal(tiempos.Order().ToArray(), tiempos);
        Assert.Equal(planificador.RelojMs, tiempos[^1]);
    }
}
