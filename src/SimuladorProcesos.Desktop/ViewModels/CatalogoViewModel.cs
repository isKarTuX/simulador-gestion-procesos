using System.Collections.ObjectModel;
using SimuladorProcesos.Core;
using SimuladorProcesos.Core.Modelos;
using SimuladorProcesos.Data;
using SimuladorProcesos.Desktop.Servicios;
using SimuladorProcesos.Desktop.ViewModels.Base;

namespace SimuladorProcesos.Desktop.ViewModels;

/// <summary>M2: lógica de la ventana principal. No conoce WPF; la vista solo enlaza sus propiedades y comandos.</summary>
public sealed class CatalogoViewModel : ObservableObject
{
    private readonly IRecolectorProcesos _recolector;
    private readonly ICatalogoRepositorio _repositorio;
    private readonly IActividadProceso _actividad;
    private readonly string _carpetaActividad;

    private int? _cantidad = 10;
    private CriterioSeleccion _criterio = CriterioSeleccion.Cpu;
    private bool _usarConsecutivo = true;
    private int? _numero;
    private string _nombreCatalogo = string.Empty;
    private int? _thMs = 100;
    private bool _ocupado;
    private string _mensaje = "Listo. Define N y el criterio, y captura los procesos.";
    private bool _mensajeEsError;
    private CatalogoGuardadoViewModel? _catalogoSeleccionado;

    // Verdadero cuando las filas son una captura recién hecha que aún no está en la BD.
    private bool _capturaPendiente;
    private int _numeroSugerido = 1;
    private int _versionSeleccion;
    private CancellationTokenSource? _cancelacion;

    public CatalogoViewModel(
        IRecolectorProcesos recolector,
        ICatalogoRepositorio repositorio,
        IActividadProceso actividad,
        string? carpetaActividad = null)
    {
        _recolector = recolector;
        _repositorio = repositorio;
        _actividad = actividad;
        _carpetaActividad = carpetaActividad ?? RutasSimulador.CarpetaActividad;

        CapturarCommand = new AsyncRelayCommand(CapturarAsync, PuedeCapturar, AlFallar);
        GuardarCommand = new AsyncRelayCommand(GuardarAsync, PuedeGuardar, AlFallar);
        EjecutarActividadCommand = new AsyncRelayCommand(EjecutarActividadAsync, PuedeEjecutarActividad, AlFallar);
        CancelarCommand = new RelayCommand(Cancelar, () => Ocupado);

        Procesos.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(HayProcesos));
            OnPropertyChanged(nameof(ResumenProcesos));
            ActualizarComandos();
        };
        CatalogosGuardados.CollectionChanged += (_, _) => OnPropertyChanged(nameof(SinCatalogos));
    }

    // ---- Datos que muestra la vista ----

    public ObservableCollection<ProcesoFilaViewModel> Procesos { get; } = [];

    public ObservableCollection<CatalogoGuardadoViewModel> CatalogosGuardados { get; } = [];

    public IReadOnlyList<OpcionCriterio> Criterios { get; } =
    [
        new(CriterioSeleccion.Cpu, OpcionCriterio.TextoDe(CriterioSeleccion.Cpu)),
        new(CriterioSeleccion.Memoria, OpcionCriterio.TextoDe(CriterioSeleccion.Memoria)),
    ];

    public bool HayProcesos => Procesos.Count > 0;

    public bool SinCatalogos => CatalogosGuardados.Count == 0;

    public string ResumenProcesos => Procesos.Count == 1 ? "1 proceso" : $"{Procesos.Count} procesos";

    // ---- Formulario ----
    // Son int? porque un cuadro de texto vacío es "null" (inválido), no un 0 silencioso.

    public int? Cantidad
    {
        get => _cantidad;
        set
        {
            if (SetProperty(ref _cantidad, value))
            {
                AlCambiarFormulario();
            }
        }
    }

    public CriterioSeleccion Criterio
    {
        get => _criterio;
        set => SetProperty(ref _criterio, value);
    }

    public bool UsarConsecutivo
    {
        get => _usarConsecutivo;
        set
        {
            if (!SetProperty(ref _usarConsecutivo, value))
            {
                return;
            }

            OnPropertyChanged(nameof(NumeroEditable));
            if (value)
            {
                // Al volver al consecutivo se recupera el sugerido, descartando lo digitado.
                Numero = _numeroSugerido;
            }
        }
    }

    public bool NumeroEditable => !UsarConsecutivo;

    public int? Numero
    {
        get => _numero;
        set
        {
            if (SetProperty(ref _numero, value))
            {
                AlCambiarFormulario();
            }
        }
    }

    public string NombreCatalogo
    {
        get => _nombreCatalogo;
        set
        {
            if (SetProperty(ref _nombreCatalogo, value ?? string.Empty))
            {
                AlCambiarFormulario();
            }
        }
    }

    /// <summary>TH: milisegundos que espera la actividad entre carácter y carácter.</summary>
    public int? ThMs
    {
        get => _thMs;
        set
        {
            if (SetProperty(ref _thMs, value))
            {
                AlCambiarFormulario();
            }
        }
    }

    // ---- Validaciones visibles (cadena vacía = sin error) ----

    public string ErrorCantidad => Cantidad is > 0 ? string.Empty : "N debe ser un entero mayor que 0.";

    public string ErrorNumero => Numero is > 0 ? string.Empty : "El número debe ser mayor que 0.";

    public string ErrorNombre => string.IsNullOrWhiteSpace(NombreCatalogo) ? "El nombre del catálogo no puede estar vacío." : string.Empty;

    public string ErrorTh => ThMs is > 0 ? string.Empty : "TH debe ser mayor que 0 ms.";

    public bool FormularioValido => ErrorCantidad.Length == 0 && ErrorNumero.Length == 0 && ErrorNombre.Length == 0;

    // ---- Estado de la barra inferior ----

    public bool Ocupado
    {
        get => _ocupado;
        private set
        {
            if (SetProperty(ref _ocupado, value))
            {
                OnPropertyChanged(nameof(NoOcupado));
                ActualizarComandos();
            }
        }
    }

    public bool NoOcupado => !Ocupado;

    public string Mensaje
    {
        get => _mensaje;
        private set => SetProperty(ref _mensaje, value);
    }

    public bool MensajeEsError
    {
        get => _mensajeEsError;
        private set => SetProperty(ref _mensajeEsError, value);
    }

    // ---- Catálogo guardado seleccionado en el panel lateral ----

    public CatalogoGuardadoViewModel? CatalogoSeleccionado
    {
        get => _catalogoSeleccionado;
        set
        {
            if (SetProperty(ref _catalogoSeleccionado, value))
            {
                // El setter no puede esperar; MostrarCatalogoGuardadoAsync atrapa sus propios errores.
                _ = MostrarCatalogoGuardadoAsync(value);
            }
        }
    }

    // ---- Comandos ----

    public AsyncRelayCommand CapturarCommand { get; }

    public AsyncRelayCommand GuardarCommand { get; }

    public AsyncRelayCommand EjecutarActividadCommand { get; }

    public RelayCommand CancelarCommand { get; }

    /// <summary>Al abrir la ventana: sugiere el siguiente número y carga los catálogos ya guardados.</summary>
    public async Task InicializarAsync()
    {
        try
        {
            await RecargarCatalogosAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            Informar($"No se pudo leer la base de datos: {ex.Message}", esError: true);
        }
    }

    /// <summary>Muestra en el grid los procesos de un catálogo guardado.</summary>
    public async Task MostrarCatalogoGuardadoAsync(CatalogoGuardadoViewModel? elegido)
    {
        // Si el usuario cambia de selección mientras se lee, solo vale la última respuesta.
        var version = ++_versionSeleccion;
        if (elegido is null)
        {
            return;
        }

        try
        {
            var catalogo = await _repositorio.ObtenerConProcesosAsync(elegido.Id);
            if (version != _versionSeleccion)
            {
                return;
            }

            if (catalogo is null)
            {
                Informar($"El catálogo {elegido.Numero} ya no existe.", esError: true);
                return;
            }

            ReemplazarFilas(catalogo.Procesos);
            _capturaPendiente = false;
            ActualizarComandos();
            Informar($"Catálogo {catalogo.Numero} «{catalogo.Nombre}»: {Procesos.Count} procesos.");
        }
        catch (Exception ex)
        {
            Informar($"No se pudo abrir el catálogo: {ex.Message}", esError: true);
        }
    }

    // ---- Operaciones ----

    private Task CapturarAsync() => EjecutarOperacionAsync(async cancelacion =>
    {
        if (ErrorCantidad.Length > 0)
        {
            Informar(ErrorCantidad, esError: true);
            return;
        }

        var pedidos = Cantidad!.Value;
        Informar($"Capturando {pedidos} procesos por {OpcionCriterio.TextoDe(Criterio)}...");
        var capturados = await _recolector.CapturarAsync(pedidos, Criterio, cancelacion);

        _versionSeleccion++;
        ReemplazarFilas(capturados);
        SeleccionarSinCargar(null);
        _capturaPendiente = capturados.Count > 0;

        Informar(capturados.Count < pedidos
            ? $"Solo hay {capturados.Count} procesos disponibles de los {pedidos} pedidos."
            : $"{capturados.Count} procesos capturados por {OpcionCriterio.TextoDe(Criterio)}.");
    });

    private Task GuardarAsync() => EjecutarOperacionAsync(async cancelacion =>
    {
        // El botón ya está deshabilitado en estos casos; se repite aquí para no depender de la vista.
        var error = PrimerErrorDelFormulario();
        if (error is not null)
        {
            Informar(error, esError: true);
            return;
        }

        if (!_capturaPendiente || Procesos.Count == 0)
        {
            Informar("Primero captura los procesos que quieres guardar.", esError: true);
            return;
        }

        var catalogo = new Catalogo
        {
            Numero = Numero!.Value,
            Nombre = NombreCatalogo.Trim(),
            Fecha = DateTime.Now,
            Criterio = Criterio,
            Cantidad = Cantidad!.Value,
            Procesos = [.. Procesos.Select(fila => fila.Modelo)],
        };

        try
        {
            var guardado = await _repositorio.GuardarAsync(catalogo, cancelacion);
            _capturaPendiente = false;
            await RecargarCatalogosAsync(cancelacion);
            SeleccionarSinCargar(CatalogosGuardados.FirstOrDefault(c => c.Id == guardado.Id));
            Informar($"Catálogo {guardado.Numero} «{guardado.Nombre}» guardado con {Procesos.Count} procesos.");
        }
        catch (InvalidOperationException ex)
        {
            // El repositorio usa esta excepción para el número repetido: se muestra tal cual al usuario.
            Informar(ex.Message, esError: true);
        }
    });

    private Task EjecutarActividadAsync() => EjecutarOperacionAsync(async cancelacion =>
    {
        if (ErrorTh.Length > 0)
        {
            Informar(ErrorTh, esError: true);
            return;
        }

        var th = ThMs!.Value;
        var filas = Procesos.ToList();
        foreach (var fila in filas)
        {
            fila.ProgresoActividad = 0;
        }

        Informar($"Ejecutando la actividad de {filas.Count} procesos en paralelo (TH = {th} ms)...");
        var fallos = await Task.WhenAll(filas.Select(fila => EjecutarFilaAsync(fila, th, cancelacion)));

        var errores = fallos.Where(f => f is not null).ToList();
        if (errores.Count == 0)
        {
            Informar($"Actividad completa: {filas.Count} archivos en {_carpetaActividad}");
        }
        else
        {
            Informar($"Actividad terminada con {errores.Count} fallo(s). {errores[0]}", esError: true);
        }
    });

    /// <summary>Devuelve null si todo salió bien, o el texto del error de ese proceso (uno que falle no frena a los demás).</summary>
    private async Task<string?> EjecutarFilaAsync(ProcesoFilaViewModel fila, int th, CancellationToken cancelacion)
    {
        var total = fila.Descripcion.Length;
        var progreso = new ProgresoSincronizado(copiados =>
        {
            // Math.Max: un aviso tardío nunca hace retroceder la barra.
            var porcentaje = total == 0 ? 100 : copiados * 100.0 / total;
            fila.ProgresoActividad = Math.Max(fila.ProgresoActividad, porcentaje);
        });

        try
        {
            await _actividad.EjecutarAsync(fila.Modelo, th, _carpetaActividad, progreso, cancelacion);
            fila.ProgresoActividad = 100;
            return null;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return $"{fila.Nombre}: {ex.Message}";
        }
    }

    private void Cancelar() => _cancelacion?.Cancel();

    /// <summary>Marco común: marca Ocupado, crea el token de cancelación y traduce cualquier fallo en un mensaje.</summary>
    private async Task EjecutarOperacionAsync(Func<CancellationToken, Task> operacion)
    {
        using var cancelacion = new CancellationTokenSource();
        _cancelacion = cancelacion;
        Ocupado = true;
        try
        {
            await operacion(cancelacion.Token);
        }
        catch (OperationCanceledException)
        {
            Informar("Operación cancelada.");
        }
        catch (Exception ex)
        {
            Informar(ex.Message, esError: true);
        }
        finally
        {
            _cancelacion = null;
            Ocupado = false;
        }
    }

    private async Task RecargarCatalogosAsync(CancellationToken cancelacion)
    {
        var catalogos = await _repositorio.ListarAsync(cancelacion);
        _numeroSugerido = await _repositorio.SiguienteNumeroAsync(cancelacion);

        CatalogosGuardados.Clear();
        foreach (var catalogo in catalogos.OrderBy(c => c.Numero))
        {
            CatalogosGuardados.Add(new CatalogoGuardadoViewModel(catalogo));
        }

        Numero = _numeroSugerido;
    }

    private void ReemplazarFilas(IEnumerable<ProcesoCatalogo> procesos)
    {
        Procesos.Clear();
        foreach (var proceso in procesos)
        {
            Procesos.Add(new ProcesoFilaViewModel(proceso));
        }
    }

    /// <summary>Cambia la selección del panel lateral sin disparar la lectura del catálogo.</summary>
    private void SeleccionarSinCargar(CatalogoGuardadoViewModel? elegido)
    {
        _catalogoSeleccionado = elegido;
        OnPropertyChanged(nameof(CatalogoSeleccionado));
    }

    // ---- Habilitación de comandos y validación ----

    private bool PuedeCapturar() => !Ocupado && ErrorCantidad.Length == 0;

    private bool PuedeGuardar() => !Ocupado && _capturaPendiente && HayProcesos && FormularioValido;

    private bool PuedeEjecutarActividad() => !Ocupado && HayProcesos && ErrorTh.Length == 0;

    private string? PrimerErrorDelFormulario() =>
        new[] { ErrorCantidad, ErrorNumero, ErrorNombre }.FirstOrDefault(e => e.Length > 0);

    private void AlCambiarFormulario()
    {
        OnPropertyChanged(nameof(ErrorCantidad));
        OnPropertyChanged(nameof(ErrorNumero));
        OnPropertyChanged(nameof(ErrorNombre));
        OnPropertyChanged(nameof(ErrorTh));
        OnPropertyChanged(nameof(FormularioValido));
        ActualizarComandos();
    }

    private void ActualizarComandos()
    {
        CapturarCommand.RaiseCanExecuteChanged();
        GuardarCommand.RaiseCanExecuteChanged();
        EjecutarActividadCommand.RaiseCanExecuteChanged();
        CancelarCommand.RaiseCanExecuteChanged();
    }

    private void Informar(string mensaje, bool esError = false)
    {
        Mensaje = mensaje;
        MensajeEsError = esError;
    }

    private void AlFallar(Exception ex) => Informar($"Error inesperado: {ex.Message}", esError: true);
}
