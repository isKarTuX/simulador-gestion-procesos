using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using SimuladorProcesos.Core.Contrato;

namespace SimuladorProcesos.Api.Errores;

/// <summary>Toda respuesta de error del servicio es un &lt;Error&gt; en XML (CONTEXT §5.2).</summary>
public static class RespuestasError
{
    public const string TipoXml = "application/xml";

    public static ObjectResult Crear(int codigo, string mensaje) =>
        new(new ErrorXml { Codigo = codigo, Mensaje = mensaje })
        {
            StatusCode = codigo,
            ContentTypes = { TipoXml },
        };

    /// <summary>Para el middleware, que no tiene controlador: reutiliza el mismo formateador XML.</summary>
    public static Task EscribirAsync(HttpContext contexto, int codigo, string mensaje)
    {
        var accion = new ActionContext(contexto, contexto.GetRouteData(), new ActionDescriptor());
        return Crear(codigo, mensaje).ExecuteResultAsync(accion);
    }
}
