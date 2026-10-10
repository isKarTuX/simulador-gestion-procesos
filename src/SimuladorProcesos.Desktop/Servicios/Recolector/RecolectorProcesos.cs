using SimuladorProcesos.Core.Modelos;
using SimuladorProcesos.Core.Reglas;

namespace SimuladorProcesos.Desktop.Servicios.Recolector;

/// <summary>M1: arma los ProcesoCatalogo de los N procesos con mayor uso del criterio.</summary>
public sealed class RecolectorProcesos : IRecolectorProcesos
{
    private readonly ILectorProcesos _lector;
    private readonly IProveedorMetadatos _metadatos;

    public RecolectorProcesos(ILectorProcesos lector, IProveedorMetadatos metadatos)
    {
        _lector = lector ?? throw new ArgumentNullException(nameof(lector));
        _metadatos = metadatos ?? throw new ArgumentNullException(nameof(metadatos));
    }

    public async Task<IReadOnlyList<ProcesoCatalogo>> CapturarAsync(
        int cantidad,
        CriterioSeleccion criterio,
        CancellationToken cancelacion = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(cantidad, 0);

        var muestras = await _lector.LeerAsync(criterio, cancelacion);
        var candidatos = SelectorProcesos.Ordenar(muestras, criterio);

        // WMI es lento: la consulta de metadatos va fuera del hilo de la UI.
        return await Task.Run(() => Completar(candidatos, cantidad, cancelacion), cancelacion);
    }

    private List<ProcesoCatalogo> Completar(IReadOnlyList<MuestraProceso> candidatos, int cantidad, CancellationToken cancelacion)
    {
        var resultado = new List<ProcesoCatalogo>(Math.Min(cantidad, candidatos.Count));

        foreach (var candidato in candidatos)
        {
            if (resultado.Count == cantidad)
            {
                break;
            }

            cancelacion.ThrowIfCancellationRequested();

            // null = el proceso terminó entre la lectura y la consulta: se salta y se sigue con el siguiente.
            var datos = _metadatos.Obtener(candidato.Pid, candidato.Nombre);
            if (datos is null)
            {
                continue;
            }

            resultado.Add(new ProcesoCatalogo
            {
                Pid = candidato.Pid,
                Nombre = candidato.Nombre,
                Usuario = datos.Usuario,
                Descripcion = datos.Descripcion,
                Prioridad = ReglaPrioridad.Determinar(datos.Usuario),
            });
        }

        return resultado;
    }
}
