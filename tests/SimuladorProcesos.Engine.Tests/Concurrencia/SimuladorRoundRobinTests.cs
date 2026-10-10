using System.Reflection;
using SimuladorProcesos.Core.Modelos;
using SimuladorProcesos.Engine.Actividad;
using SimuladorProcesos.Engine.Tests.Planificacion;
using static SimuladorProcesos.Engine.Tests.Concurrencia.AuxiliarSimulador;

namespace SimuladorProcesos.Engine.Tests.Concurrencia;

/// <summary>
/// Motor con hilos (ADR-0002) a TH = 1 ms. El resultado se compara contra el reloj simulado y contra la lógica sin hilos,
/// nunca contra el reloj de pared, así que las pruebas no dependen de la velocidad del equipo.
/// </summary>
[Trait("Category", "Concurrencia")]
public class SimuladorRoundRobinTests
{
    [Theory]
    [MemberData(nameof(Catalogos), MemberType = typeof(AuxiliarSimulador))]
    public void El_reporte_y_la_traza_coinciden_con_el_planificador_sin_hilos(string catalogo)
    {
        var (procesos, quantum) = Catalogo(catalogo);
        var (reporteEsperado, trazaEsperada) = ReferenciaSinHilos(procesos, quantum);
        var traza = new List<string>();
        using var simulador = new SimuladorRoundRobin(new DestinoEnMemoria());
        simulador.CambioEstado += (_, e) => traza.Add(AuxiliarPlanificador.Formato(e));

        simulador.Cargar(procesos, new ConfiguracionSimulacion(ThMs, quantum));
        EjecutarHastaElFinal(simulador);

        Assert.Equal(EstadoSimulacion.Finalizada, simulador.Estado);
        Assert.Equal(reporteEsperado, simulador.ObtenerReporte());
        Assert.Equal(trazaEsperada, traza);
        Assert.Equal(procesos.Sum(p => p.Descripcion.Length) * ThMs, simulador.ObtenerInstantanea().RelojMs);
    }

    [Fact]
    public void Un_catalogo_expulsivo_da_el_reporte_calculado_a_mano()
    {
        // TH = 1, quantum = 3. Ana = "abcdefgh" (8), Beto = "ij" (2), Carla = "klmnopq" (7). Total 17 caracteres.
        //   turno | proceso | copia | reloj | consecuencia
        //   1     | Ana     | abc   | 3     | vuelve a Listo
        //   2     | Beto    | ij    | 5     | Terminado
        //   3     | Carla   | klm   | 8     | vuelve a Listo
        //   4     | Ana     | def   | 11    | vuelve a Listo
        //   5     | Carla   | nop   | 14    | vuelve a Listo
        //   6     | Ana     | gh    | 16    | Terminado
        //   7     | Carla   | q     | 17    | Terminado
        // TL = 0, 1, 2. Ejecuciones: Ana 3, Beto 1, Carla 3. T.F del enunciado = 3 x ejecuciones.
        // T.R = TF real - TL: Beto 5-1 = 4, Ana 16-0 = 16, Carla 17-2 = 15. Espera = T.R - R: 2, 8, 8.
        var (procesos, quantum) = Catalogo("expulsivos");
        using var simulador = Cargado(new DestinoEnMemoria(), procesos, quantum);

        EjecutarHastaElFinal(simulador);

        Assert.Equal(
            [
                new FilaReporte("Beto", TiempoLlegada: 1, Rafaga: 2, PrioridadAsignada: 0, Turnaround: 4, TiempoFinalizacion: 3, TiempoFinalizacionReal: 5, Espera: 2),
                new FilaReporte("Ana", TiempoLlegada: 0, Rafaga: 8, PrioridadAsignada: 0, Turnaround: 16, TiempoFinalizacion: 9, TiempoFinalizacionReal: 16, Espera: 8),
                new FilaReporte("Carla", TiempoLlegada: 2, Rafaga: 7, PrioridadAsignada: 0, Turnaround: 15, TiempoFinalizacion: 9, TiempoFinalizacionReal: 17, Espera: 8),
            ],
            simulador.ObtenerReporte());
    }

    [Theory]
    [MemberData(nameof(Catalogos), MemberType = typeof(AuxiliarSimulador))]
    public void Cada_proceso_escribe_exactamente_su_descripcion(string catalogo)
    {
        var (procesos, quantum) = Catalogo(catalogo);
        var destino = new DestinoEnMemoria();
        using var simulador = Cargado(destino, procesos, quantum);

        EjecutarHastaElFinal(simulador);

        foreach (var proceso in procesos)
        {
            Assert.Equal(proceso.Descripcion, destino.Texto(proceso.Pid));
        }
    }

    [Fact]
    public void Nunca_hay_dos_procesos_en_Ejecucion_y_cada_transicion_es_legal()
    {
        var (procesos, quantum) = Catalogo("muchos");
        var enEjecucion = 0;
        var maximo = 0;
        var ilegales = new List<string>();
        var estadoPorProceso = new Dictionary<int, EstadoProceso>();
        using var simulador = new SimuladorRoundRobin(new DestinoEnMemoria());

        // Los eventos llegan en orden desde un solo hilo; los fallos se anotan y se afirman en el hilo de la prueba.
        simulador.CambioEstado += (_, e) =>
        {
            var conocido = estadoPorProceso.TryGetValue(e.Proceso.Pid, out var anterior);
            var legal = (e.Desde, e.Hacia) switch
            {
                (null, EstadoProceso.Listo) => !conocido,
                (EstadoProceso.Listo, EstadoProceso.Ejecucion) => conocido && anterior == EstadoProceso.Listo,
                (EstadoProceso.Ejecucion, EstadoProceso.Listo) => conocido && anterior == EstadoProceso.Ejecucion,
                (EstadoProceso.Ejecucion, EstadoProceso.Terminado) => conocido && anterior == EstadoProceso.Ejecucion,
                _ => false,
            };
            if (!legal)
            {
                ilegales.Add($"{e.Proceso.Nombre}: {e.Desde} > {e.Hacia} (estaba {(conocido ? anterior : "sin estado")})");
            }

            estadoPorProceso[e.Proceso.Pid] = e.Hacia;
            if (e.Hacia == EstadoProceso.Ejecucion)
            {
                maximo = Math.Max(maximo, ++enEjecucion);
            }
            else if (e.Desde == EstadoProceso.Ejecucion)
            {
                enEjecucion--;
            }
        };
        simulador.Cargar(procesos, new ConfiguracionSimulacion(ThMs, quantum));

        EjecutarHastaElFinal(simulador);

        Assert.Empty(ilegales);
        Assert.Equal(1, maximo);
        Assert.Equal(0, enEjecucion);
        Assert.Equal(procesos.Length, estadoPorProceso.Count);
        Assert.All(estadoPorProceso.Values, estado => Assert.Equal(EstadoProceso.Terminado, estado));
    }

    [Fact]
    public void SimulacionFinalizada_se_dispara_una_sola_vez_con_el_estado_ya_Finalizada()
    {
        var (procesos, quantum) = Catalogo("mezcla");
        using var simulador = Cargado(new DestinoEnMemoria(), procesos, quantum);
        var avisos = new List<EstadoSimulacion>();
        simulador.SimulacionFinalizada += (_, _) => avisos.Add(simulador.Estado);

        EjecutarHastaElFinal(simulador);
        Thread.Sleep(50);

        Assert.Equal([EstadoSimulacion.Finalizada], avisos);
    }

    [Fact]
    public void Al_terminar_ningun_hilo_sigue_vivo()
    {
        var (procesos, quantum) = Catalogo("mezcla");
        using var simulador = Cargado(new DestinoEnMemoria(), procesos, quantum);
        simulador.Iniciar();
        var hilos = simulador.Hilos;

        Assert.All(hilos, hilo => Assert.True(hilo.Join(Limite), $"El hilo {hilo.Name} no terminó."));
        Assert.Equal(EstadoSimulacion.Finalizada, simulador.Estado);
        Assert.Equal(
            procesos.Select(p => $"Proceso-{p.Pid}").Append("Dispatcher").Order(StringComparer.Ordinal),
            hilos.Select(h => h.Name!).Order(StringComparer.Ordinal));
    }

    [Fact]
    public void Pausar_desde_el_propio_proceso_congela_todo_y_Reanudar_termina_la_simulacion()
    {
        var descripcion = Texto(120);
        var procesos = new[] { AuxiliarPlanificador.Proceso("Largo", descripcion, Prioridad.Expulsivo) };
        using var pausada = new ManualResetEventSlim();
        SimuladorRoundRobin? referencia = null;

        // El gancho pausa tras el carácter 10, así que el punto de la pausa es exacto y no depende de la velocidad del equipo.
        var destino = new DestinoConGancho(total =>
        {
            if (total == 10)
            {
                referencia!.Pausar();
                pausada.Set();
            }
        });
        using var simulador = Cargado(destino, procesos, quantum: 7);
        referencia = simulador;
        simulador.Iniciar();
        Assert.True(pausada.Wait(Limite));

        var antes = Resumen(simulador.ObtenerInstantanea());
        Thread.Sleep(100);
        var despues = Resumen(simulador.ObtenerInstantanea());

        Assert.Equal(antes, despues);
        Assert.Equal(EstadoSimulacion.Pausada, simulador.Estado);
        Assert.Equal(10, destino.Total);
        Assert.Equal(10 * ThMs, simulador.ObtenerInstantanea().RelojMs);

        simulador.Pausar();
        Assert.Equal(EstadoSimulacion.Pausada, simulador.Estado);

        using var fin = new ManualResetEventSlim();
        simulador.SimulacionFinalizada += (_, _) => fin.Set();
        simulador.Reanudar();
        Assert.True(fin.Wait(Limite), "Reanudar no llevó la simulación hasta el final.");

        Assert.Equal(EstadoSimulacion.Finalizada, simulador.Estado);
        Assert.Equal(120 * ThMs, simulador.ObtenerInstantanea().RelojMs);
        Assert.Equal(descripcion, destino.Texto(procesos[0].Pid));
    }

    [Fact]
    public void Pausar_desde_otro_hilo_nunca_deja_cambiar_la_instantanea_despues_de_retornar()
    {
        // Pausa y reanuda muchas veces en puntos arbitrarios: si un hilo ya había pasado la compuerta, la foto cambiaría tras Pausar.
        var procesos = new[]
        {
            AuxiliarPlanificador.Proceso("Ana", Texto(60), Prioridad.Expulsivo),
            AuxiliarPlanificador.Proceso("Beto", Texto(40), Prioridad.Expulsivo),
            AuxiliarPlanificador.Proceso("Carla", Texto(30), Prioridad.NoExpulsivo),
        };
        var (reporteEsperado, _) = ReferenciaSinHilos(procesos, 5);
        using var simulador = Cargado(new DestinoEnMemoria(), procesos, 5);
        using var fin = new ManualResetEventSlim();
        simulador.SimulacionFinalizada += (_, _) => fin.Set();
        simulador.Iniciar();

        var pausas = 0;
        var limite = DateTime.UtcNow + Limite;
        while (!fin.IsSet && DateTime.UtcNow < limite)
        {
            try
            {
                simulador.Pausar();
            }
            catch (InvalidOperationException)
            {
                break; // La simulación acabó justo antes de pausar.
            }

            pausas++;
            var primera = Resumen(simulador.ObtenerInstantanea());
            Thread.Sleep(5);
            Assert.Equal(primera, Resumen(simulador.ObtenerInstantanea()));

            simulador.Reanudar();
            Thread.Sleep(10);
        }

        Assert.True(fin.Wait(Limite), "La simulación no terminó.");
        Assert.True(pausas > 0);
        Assert.Equal(reporteEsperado, simulador.ObtenerReporte());
    }

    [Fact]
    public void Si_Pausar_gana_el_lock_a_hilos_que_ya_pasaron_la_compuerta_ninguno_avanza()
    {
        // Un solo turno largo: entre carácter y carácter lo único que el hilo pide al lock es copiar, que es lo que se debe congelar.
        var procesos = new[] { AuxiliarPlanificador.Proceso("Ana", Texto(400), Prioridad.Expulsivo) };
        using var simulador = Cargado(new DestinoEnMemoria(), procesos, 1000);
        var candado = typeof(SimuladorRoundRobin).GetField("_estado", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(simulador)!;
        simulador.Iniciar();
        Esperar(() => simulador.ObtenerInstantanea().RelojMs >= 2, "El reloj no avanzó.");

        string antes;
        lock (candado)
        {
            // Con el lock tomado los hilos del motor llegan a él con la compuerta todavía abierta y se quedan esperando.
            Thread.Sleep(50);

            // El lock es reentrante: Pausar gana el lock a esos hilos y cierra la compuerta antes de que entren.
            simulador.Pausar();
            antes = Resumen(simulador.ObtenerInstantanea());
        }

        Thread.Sleep(50);
        Assert.Equal(antes, Resumen(simulador.ObtenerInstantanea()));
    }

    [Fact]
    public void Reiniciar_a_mitad_no_deja_hilos_vivos_y_una_corrida_nueva_da_el_mismo_reporte()
    {
        var procesos = new[]
        {
            AuxiliarPlanificador.Proceso("Largo", Texto(300), Prioridad.Expulsivo),
            AuxiliarPlanificador.Proceso("Corto", Texto(10), Prioridad.NoExpulsivo),
        };
        var (reporteEsperado, _) = ReferenciaSinHilos(procesos, 50);
        var destino = new DestinoEnMemoria();
        using var simulador = Cargado(destino, procesos, 50);
        simulador.Iniciar();
        Esperar(() => simulador.ObtenerInstantanea().RelojMs >= 5, "El reloj no avanzó.");
        var hilos = simulador.Hilos;

        // Si esto falla la corrida ya había terminado y la prueba no estaría reiniciando "a mitad".
        Assert.Equal(EstadoSimulacion.EnCurso, simulador.Estado);

        simulador.Reiniciar();

        Assert.Equal(
            procesos.Select(p => $"Proceso-{p.Pid}").Append("Dispatcher").Order(StringComparer.Ordinal),
            hilos.Select(h => h.Name!).Order(StringComparer.Ordinal));
        Assert.DoesNotContain(hilos, h => h.IsAlive);
        Assert.Empty(simulador.Hilos);
        Assert.Equal(EstadoSimulacion.Detenida, simulador.Estado);
        var foto = simulador.ObtenerInstantanea();
        Assert.Equal(0, foto.RelojMs);
        Assert.Equal(["Largo", "Corto"], foto.Listos.Select(p => p.Nombre));
        Assert.Null(foto.Ejecucion);
        Assert.Empty(foto.Terminados);
        Assert.Empty(simulador.ObtenerReporte());

        EjecutarHastaElFinal(simulador);

        Assert.Equal(reporteEsperado, simulador.ObtenerReporte());
        Assert.Equal(procesos[0].Descripcion, destino.Texto(procesos[0].Pid));
        Assert.Equal(procesos[1].Descripcion, destino.Texto(procesos[1].Pid));
    }

    [Fact]
    public void Reiniciar_estando_pausada_libera_los_hilos_bloqueados_en_la_pausa()
    {
        var procesos = new[]
        {
            AuxiliarPlanificador.Proceso("Ana", Texto(100), Prioridad.Expulsivo),
            AuxiliarPlanificador.Proceso("Beto", Texto(100), Prioridad.Expulsivo),
        };
        var (reporteEsperado, _) = ReferenciaSinHilos(procesos, 10);
        using var pausada = new ManualResetEventSlim();
        SimuladorRoundRobin? referencia = null;

        // Solo pausa en el carácter 15 de toda la prueba: la corrida posterior ya pasa de ese total y no se pausa sola.
        var destino = new DestinoConGancho(total =>
        {
            if (total == 15)
            {
                referencia!.Pausar();
                pausada.Set();
            }
        });
        using var simulador = Cargado(destino, procesos, 10);
        referencia = simulador;
        simulador.Iniciar();
        Assert.True(pausada.Wait(Limite));
        var hilos = simulador.Hilos;

        simulador.Reiniciar();

        Assert.DoesNotContain(hilos, h => h.IsAlive);
        Assert.Equal(EstadoSimulacion.Detenida, simulador.Estado);
        Assert.Equal(0, simulador.ObtenerInstantanea().RelojMs);

        EjecutarHastaElFinal(simulador);

        Assert.Equal(reporteEsperado, simulador.ObtenerReporte());
    }

    [Fact]
    public void Dispose_durante_la_ejecucion_no_se_cuelga_y_no_deja_hilos_vivos()
    {
        var procesos = new[]
        {
            AuxiliarPlanificador.Proceso("Ana", Texto(300), Prioridad.Expulsivo),
            AuxiliarPlanificador.Proceso("Beto", Texto(300), Prioridad.NoExpulsivo),
        };
        var simulador = Cargado(new DestinoEnMemoria(), procesos, 20);
        simulador.Iniciar();
        Esperar(() => simulador.ObtenerInstantanea().RelojMs >= 3, "El reloj no avanzó.");
        var hilos = simulador.Hilos;

        // Un hilo aparte con Join acotado: si Dispose se cuelga, la prueba falla en 5 s en vez de colgarse con él.
        var cierre = new Thread(simulador.Dispose);
        cierre.Start();

        Assert.True(cierre.Join(TimeSpan.FromSeconds(5)), "Dispose se colgó.");
        Assert.DoesNotContain(hilos, h => h.IsAlive);
    }

    [Fact]
    public void Dispose_estando_pausada_no_se_cuelga_y_no_deja_hilos_vivos()
    {
        var procesos = new[] { AuxiliarPlanificador.Proceso("Ana", Texto(300), Prioridad.Expulsivo) };
        var simulador = Cargado(new DestinoEnMemoria(), procesos, 20);
        simulador.Iniciar();
        Esperar(() => simulador.ObtenerInstantanea().RelojMs >= 3, "El reloj no avanzó.");
        simulador.Pausar();
        var hilos = simulador.Hilos;

        // Un hilo aparte con Join acotado: si Dispose se cuelga, la prueba falla en 5 s en vez de colgarse con él.
        var cierre = new Thread(simulador.Dispose);
        cierre.Start();

        Assert.True(cierre.Join(TimeSpan.FromSeconds(5)), "Dispose se colgó.");
        Assert.DoesNotContain(hilos, h => h.IsAlive);
    }

    [Fact]
    public void Reiniciar_desde_un_handler_del_propio_dispatcher_no_se_cuelga()
    {
        var (procesos, quantum) = Catalogo("mezcla");
        var (reporteEsperado, _) = ReferenciaSinHilos(procesos, quantum);
        using var simulador = new SimuladorRoundRobin(new DestinoEnMemoria());
        using var reiniciado = new ManualResetEventSlim();
        var pedidos = 0;
        simulador.CambioEstado += (_, e) =>
        {
            // El primer Listo > Ejecucion ocurre en el hilo dispatcher: unirse a sí mismo colgaría Reiniciar.
            if (e.Hacia == EstadoProceso.Ejecucion && Interlocked.Increment(ref pedidos) == 1)
            {
                simulador.Reiniciar();
                reiniciado.Set();
            }
        };
        simulador.Cargar(procesos, new ConfiguracionSimulacion(ThMs, quantum));

        simulador.Iniciar();

        Assert.True(reiniciado.Wait(TimeSpan.FromSeconds(10)), "Reiniciar desde el handler se colgó.");
        Assert.Equal(EstadoSimulacion.Detenida, simulador.Estado);
        EjecutarHastaElFinal(simulador);
        Assert.Equal(reporteEsperado, simulador.ObtenerReporte());
    }

    [Fact]
    public void Un_handler_que_lanza_no_detiene_la_simulacion_ni_a_los_demas_handlers()
    {
        var (procesos, quantum) = Catalogo("mezcla");
        var (reporteEsperado, trazaEsperada) = ReferenciaSinHilos(procesos, quantum);
        var traza = new List<string>();
        using var simulador = new SimuladorRoundRobin(new DestinoEnMemoria());
        using var fin = new ManualResetEventSlim();

        // Los dos primeros suscriptores fallan; el tercero debe seguir recibiendo todo.
        simulador.CambioEstado += (_, _) => throw new ObjectDisposedException("circuito cerrado");
        simulador.CambioEstado += (_, e) => traza.Add(AuxiliarPlanificador.Formato(e));
        simulador.SimulacionFinalizada += (_, _) => throw new InvalidOperationException("handler defectuoso");
        simulador.SimulacionFinalizada += (_, _) => fin.Set();
        simulador.Cargar(procesos, new ConfiguracionSimulacion(ThMs, quantum));

        simulador.Iniciar();

        Assert.True(fin.Wait(Limite), "La simulación no terminó con un handler que lanza.");
        Assert.Equal(reporteEsperado, simulador.ObtenerReporte());
        Assert.Equal(trazaEsperada, traza);
    }

    [Fact]
    public void Un_destino_que_lanza_no_detiene_la_simulacion()
    {
        var (procesos, quantum) = Catalogo("mezcla");
        var (reporteEsperado, _) = ReferenciaSinHilos(procesos, quantum);
        using var simulador = Cargado(new QA.DestinoQueLanza(), procesos, quantum);

        EjecutarHastaElFinal(simulador);

        Assert.Equal(reporteEsperado, simulador.ObtenerReporte());
    }

    [Fact]
    public void Un_fallo_inesperado_en_un_hilo_cancela_la_corrida_sin_colgar_a_los_demas_y_deja_reintentar()
    {
        var procesos = new[] { AuxiliarPlanificador.Proceso("Ana", Texto(300), Prioridad.Expulsivo) };
        var (reporteEsperado, _) = ReferenciaSinHilos(procesos, 5);
        using var simulador = Cargado(new DestinoEnMemoria(), procesos, 5);
        simulador.Iniciar();
        Esperar(() => simulador.ObtenerInstantanea().RelojMs >= 1, "El reloj no avanzó.");
        var hilos = simulador.Hilos;

        // Se provoca un fallo del propio motor (no de un handler ni del destino): un semáforo de turno liberado de golpe.
        var corrida = typeof(SimuladorRoundRobin).GetField("_corrida", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(simulador)!;
        var turnos = (SemaphoreSlim[])corrida.GetType().GetProperty("Turnos")!.GetValue(corrida)!;
        turnos[0].Dispose();

        Assert.All(hilos, hilo => Assert.True(hilo.Join(Limite), $"El hilo {hilo.Name} quedó esperando para siempre."));
        Assert.Equal(EstadoSimulacion.Detenida, simulador.Estado);
        Assert.Equal(0, simulador.ObtenerInstantanea().RelojMs);

        EjecutarHastaElFinal(simulador);
        Assert.Equal(reporteEsperado, simulador.ObtenerReporte());
    }

    [Fact]
    public void Pausar_y_Reanudar_nunca_lanzan_NullReference_mientras_Reiniciar_y_Dispose_corren_en_paralelo()
    {
        var inesperadas = new List<Exception>();
        for (var vuelta = 0; vuelta < 50; vuelta++)
        {
            var (procesos, quantum) = Catalogo("mezcla");
            var simulador = Cargado(new DestinoEnMemoria(), procesos, quantum);
            simulador.Iniciar();
            var hilos = new[]
            {
                new Thread(() => Intentar(simulador.Reiniciar, inesperadas)),
                new Thread(() => Intentar(simulador.Pausar, inesperadas)),
                new Thread(() => Intentar(simulador.Reanudar, inesperadas)),
                new Thread(() => Intentar(simulador.Reiniciar, inesperadas)),
            };
            foreach (var hilo in hilos)
            {
                hilo.Start();
            }

            Assert.All(hilos, hilo => Assert.True(hilo.Join(Limite)));
            simulador.Dispose();
        }

        Assert.Empty(inesperadas);
    }

    // Lo único aceptable es lo documentado: InvalidOperationException por estado inválido o ObjectDisposedException.
    private static void Intentar(Action accion, List<Exception> inesperadas)
    {
        try
        {
            accion();
        }
        catch (Exception ex) when (ex is InvalidOperationException or ObjectDisposedException)
        {
            // Respuesta válida de una operación que llegó en mal momento.
        }
        catch (Exception ex)
        {
            lock (inesperadas)
            {
                inesperadas.Add(ex);
            }
        }
    }

    [Fact]
    public void Dos_corridas_seguidas_no_mezclan_el_texto_de_la_anterior()
    {
        var (procesos, quantum) = Catalogo("expulsivos");
        var destino = new DestinoEnMemoria();
        using var simulador = Cargado(destino, procesos, quantum);

        EjecutarHastaElFinal(simulador);
        simulador.Reiniciar();
        EjecutarHastaElFinal(simulador);

        Assert.Equal(3, destino.Corridas);
        Assert.All(procesos, p => Assert.Equal(p.Descripcion, destino.Texto(p.Pid)));
    }
}
