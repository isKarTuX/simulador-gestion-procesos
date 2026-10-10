namespace SimuladorProcesos.Desktop.ViewModels.Base;

/// <summary>
/// IProgress que entrega cada valor en el contexto donde se creó (el hilo de la UI), para poder tocar propiedades enlazadas.
/// Sin contexto (pruebas) lo entrega en el acto: Progress&lt;T&gt; lo mandaría al ThreadPool y haría las pruebas no deterministas.
/// </summary>
public sealed class ProgresoSincronizado : IProgress<int>
{
    private readonly Action<int> _alReportar;
    private readonly SynchronizationContext? _contexto = SynchronizationContext.Current;

    public ProgresoSincronizado(Action<int> alReportar) => _alReportar = alReportar;

    public void Report(int valor)
    {
        if (_contexto is null)
        {
            _alReportar(valor);
        }
        else
        {
            _contexto.Post(_ => _alReportar(valor), null);
        }
    }
}
