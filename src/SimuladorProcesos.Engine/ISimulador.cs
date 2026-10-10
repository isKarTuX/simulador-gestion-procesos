namespace SimuladorProcesos.Engine;

/// <summary>
/// Contrato entre el motor (M5) y la Web (M6/M7). Cambia solo con la skill contrato-xml.
/// Ciclo: Cargar → Iniciar → (Pausar ↔ Reanudar)* → Finalizada; Reiniciar vuelve a Detenida con los mismos procesos.
/// </summary>
public interface ISimulador : IDisposable
{
    EstadoSimulacion Estado { get; }

    /// <summary>Se dispara en cada transición de un proceso, fuera de cualquier lock y desde un hilo del motor.</summary>
    event EventHandler<CambioEstadoEventArgs>? CambioEstado;

    /// <summary>Se dispara una vez cuando todos los procesos llegan a Terminado.</summary>
    event EventHandler? SimulacionFinalizada;

    /// <summary>Prepara la simulación. Solo válido en estado Detenida o Finalizada.</summary>
    void Cargar(IReadOnlyList<ProcesoEntrada> procesos, ConfiguracionSimulacion configuracion);

    void Iniciar();

    void Pausar();

    void Reanudar();

    /// <summary>Cancela los hilos, vacía las estructuras y deja los procesos cargados listos para Iniciar de nuevo.</summary>
    void Reiniciar();

    InstantaneaSimulacion ObtenerInstantanea();

    /// <summary>Una fila por proceso terminado, en orden de finalización.</summary>
    IReadOnlyList<FilaReporte> ObtenerReporte();
}
