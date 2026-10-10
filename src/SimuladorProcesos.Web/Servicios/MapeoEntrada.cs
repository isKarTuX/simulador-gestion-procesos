using SimuladorProcesos.Core.Contrato;
using SimuladorProcesos.Core.Modelos;
using SimuladorProcesos.Engine;

namespace SimuladorProcesos.Web.Servicios;

/// <summary>Traduce el XML del servicio a las entradas que consume el motor.</summary>
public static class MapeoEntrada
{
    public static IReadOnlyList<ProcesoEntrada> AProcesosEntrada(CatalogoXml catalogo)
    {
        ArgumentNullException.ThrowIfNull(catalogo);

        var entradas = new List<ProcesoEntrada>(catalogo.Procesos.Count);
        foreach (var proceso in catalogo.Procesos)
        {
            // Un entero fuera de 0/1 es un contrato roto: mejor fallar que simular con una prioridad inventada.
            if (!Enum.IsDefined((Prioridad)proceso.Prioridad))
            {
                throw new InvalidDataException($"Prioridad inválida ({proceso.Prioridad}) en el proceso {proceso.Pid}.");
            }

            entradas.Add(new ProcesoEntrada(proceso.Pid, proceso.Nombre, proceso.Descripcion, (Prioridad)proceso.Prioridad));
        }

        return entradas;
    }
}
