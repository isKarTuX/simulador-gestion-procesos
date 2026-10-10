using SimuladorProcesos.Core.Modelos;
using SimuladorProcesos.Engine.Actividad;
using SimuladorProcesos.Engine.Calculos;
using SimuladorProcesos.Engine.Planificacion;
using SimuladorProcesos.Engine.Tests.Planificacion;

namespace SimuladorProcesos.Engine.Tests.Concurrencia;

/// <summary>Ayudas de las pruebas con hilos: catálogos, referencia sin hilos y esperas acotadas.</summary>
internal static class AuxiliarSimulador
{
    // Holgura para equipos lentos o con temporizador de 15 ms (Windows): una prueba sana termina mucho antes.
    public static readonly TimeSpan Limite = TimeSpan.FromSeconds(30);

    public const int ThMs = 1;

    public static readonly string[] NombresCatalogos = ["expulsivos", "no expulsivos", "mezcla", "uno solo", "descripciones vacias", "muchos"];

    public static TheoryData<string> Catalogos
    {
        get
        {
            var datos = new TheoryData<string>();
            foreach (var nombre in NombresCatalogos)
            {
                datos.Add(nombre);
            }

            return datos;
        }
    }

    // Los pid son la inicial del nombre (AuxiliarPlanificador), así que cada catálogo usa iniciales distintas.
    public static (ProcesoEntrada[] Procesos, int Quantum) Catalogo(string nombre) => nombre switch
    {
        "expulsivos" => (
            [
                AuxiliarPlanificador.Proceso("Ana", "abcdefgh", Prioridad.Expulsivo),
                AuxiliarPlanificador.Proceso("Beto", "ij", Prioridad.Expulsivo),
                AuxiliarPlanificador.Proceso("Carla", "klmnopq", Prioridad.Expulsivo),
            ],
            3),
        "no expulsivos" => (
            [
                AuxiliarPlanificador.Proceso("Ana", "abcde", Prioridad.NoExpulsivo),
                AuxiliarPlanificador.Proceso("Beto", "fghijklm", Prioridad.NoExpulsivo),
                AuxiliarPlanificador.Proceso("Carla", "no", Prioridad.NoExpulsivo),
            ],
            2),
        "mezcla" => (
            [
                AuxiliarPlanificador.Proceso("Pablo", "hola mundo", Prioridad.Expulsivo),
                AuxiliarPlanificador.Proceso("Quique", "xy", Prioridad.NoExpulsivo),
                AuxiliarPlanificador.Proceso("Rosa", string.Empty, Prioridad.Expulsivo),
                AuxiliarPlanificador.Proceso("Sara", "abcdefg", Prioridad.Expulsivo, quantumPropio: 2),
                AuxiliarPlanificador.Proceso("Tomas", "mnopqrstu", Prioridad.NoExpulsivo),
            ],
            4),
        "uno solo" => (
            [AuxiliarPlanificador.Proceso("Unico", "descripcion", Prioridad.Expulsivo)],
            4),
        "descripciones vacias" => (
            [
                AuxiliarPlanificador.Proceso("Vera", string.Empty, Prioridad.Expulsivo),
                AuxiliarPlanificador.Proceso("Wilo", string.Empty, Prioridad.NoExpulsivo),
            ],
            3),
        "muchos" => (
            [
                .. Enumerable.Range(0, 24).Select(i => AuxiliarPlanificador.Proceso(
                    $"{(char)('A' + i)}{i}",
                    new string((char)('a' + i), (i % 4) + 1),
                    i % 3 == 0 ? Prioridad.NoExpulsivo : Prioridad.Expulsivo)),
            ],
            2),
        _ => throw new ArgumentException($"Catálogo desconocido: {nombre}", nameof(nombre)),
    };

    /// <summary>Lo que debe dar el motor con hilos: la misma lógica ejecutada paso a paso, con su traza de eventos.</summary>
    public static (IReadOnlyList<FilaReporte> Reporte, List<string> Eventos) ReferenciaSinHilos(IReadOnlyList<ProcesoEntrada> procesos, int quantum)
    {
        var eventos = new List<string>();
        var planificador = new PlanificadorRoundRobin(procesos, new ConfiguracionSimulacion(ThMs, quantum), e => eventos.Add(AuxiliarPlanificador.Formato(e)));
        AuxiliarPlanificador.EjecutarTodo(planificador);
        return (CalculadoraTiempos.Reporte(planificador.Terminados), eventos);
    }

    public static SimuladorRoundRobin Cargado(IDestinoActividad destino, IReadOnlyList<ProcesoEntrada> procesos, int quantum, int thMs = ThMs)
    {
        var simulador = new SimuladorRoundRobin(destino);
        simulador.Cargar(procesos, new ConfiguracionSimulacion(thMs, quantum));
        return simulador;
    }

    /// <summary>Inicia y espera la señal de fin; falla si no llega en el límite en vez de colgar la prueba.</summary>
    public static void EjecutarHastaElFinal(SimuladorRoundRobin simulador)
    {
        using var fin = new ManualResetEventSlim();
        void AlFinalizar(object? emisor, EventArgs e) => fin.Set();
        simulador.SimulacionFinalizada += AlFinalizar;
        try
        {
            simulador.Iniciar();
            Assert.True(fin.Wait(Limite), "La simulación no terminó dentro del límite.");
        }
        finally
        {
            simulador.SimulacionFinalizada -= AlFinalizar;
        }
    }

    public static void Esperar(Func<bool> condicion, string mensaje)
    {
        var limite = DateTime.UtcNow + Limite;
        while (!condicion())
        {
            Assert.True(DateTime.UtcNow < limite, mensaje);
            Thread.Sleep(1);
        }
    }

    /// <summary>Resumen comparable de una instantánea; los records no sirven porque sus listas se comparan por referencia.</summary>
    public static string Resumen(InstantaneaSimulacion foto)
    {
        static string Proceso(ProcesoInstantanea p) => $"{p.Nombre}:{p.Estado}:{p.CaracteresRestantes}:{p.Ejecuciones}";
        return $"{foto.Estado}|{foto.RelojMs}|L[{string.Join(",", foto.Listos.Select(Proceso))}]|E[{(foto.Ejecucion is null ? string.Empty : Proceso(foto.Ejecucion))}]|T[{string.Join(",", foto.Terminados.Select(Proceso))}]";
    }

    /// <summary>Descripción larga de n caracteres con contenido reconocible (abc...zabc...).</summary>
    public static string Texto(int caracteres) => string.Concat(Enumerable.Range(0, caracteres).Select(i => (char)('a' + (i % 26))));
}

/// <summary>Destino que avisa a la prueba tras cada carácter; permite detener el motor en un punto exacto.</summary>
internal sealed class DestinoConGancho(Action<int> trasEscribir) : IDestinoActividad
{
    private readonly DestinoEnMemoria _memoria = new();
    private int _total;

    public int Total => Volatile.Read(ref _total);

    public string Texto(int pid) => _memoria.Texto(pid);

    public void NuevaCorrida() => _memoria.NuevaCorrida();

    public void Escribir(int pid, string nombre, char caracter, CancellationToken cancelacion)
    {
        _memoria.Escribir(pid, nombre, caracter, cancelacion);
        trasEscribir(Interlocked.Increment(ref _total));
    }
}
