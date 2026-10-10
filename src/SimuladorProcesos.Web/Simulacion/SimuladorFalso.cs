using SimuladorProcesos.Engine;

namespace SimuladorProcesos.Web.Simulacion;

/// <summary>
/// ISimulador de mentira para construir la GUI sin esperar al motor: avanza con un temporizador.
/// Pendiente: subtarea 1.10 (docs/PROMPTS.md). Se reemplaza por el motor real en la subtarea 2.3.
/// </summary>
public sealed class SimuladorFalso : ISimulador
{
    public EstadoSimulacion Estado => EstadoSimulacion.Detenida;

    public event EventHandler<CambioEstadoEventArgs>? CambioEstado
    {
        add { }
        remove { }
    }

    public event EventHandler? SimulacionFinalizada
    {
        add { }
        remove { }
    }

    public void Cargar(IReadOnlyList<ProcesoEntrada> procesos, ConfiguracionSimulacion configuracion) =>
        throw new NotImplementedException("Subtarea 1.10");

    public void Iniciar() => throw new NotImplementedException("Subtarea 1.10");

    public void Pausar() => throw new NotImplementedException("Subtarea 1.10");

    public void Reanudar() => throw new NotImplementedException("Subtarea 1.10");

    public void Reiniciar() => throw new NotImplementedException("Subtarea 1.10");

    public InstantaneaSimulacion ObtenerInstantanea() => new(Estado, 0, [], null, []);

    public IReadOnlyList<FilaReporte> ObtenerReporte() => [];

    public void Dispose()
    {
    }
}
