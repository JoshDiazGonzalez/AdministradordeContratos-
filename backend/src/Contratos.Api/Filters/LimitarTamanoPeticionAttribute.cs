using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Contratos.Api.Filters;

/// <summary>
/// Rechaza con 413 y un mensaje claro las peticiones cuyo cuerpo supera el limite.
///
/// Es un filtro de recurso porque se ejecuta antes del model binding. Si se deja
/// que ASP.NET intente leer el formulario, el fallo llega convertido en un error
/// de validacion sin campo y con un texto en ingles del framework, que no dice al
/// usuario que el problema es el tamano del documento.
///
/// Complementa a [RequestSizeLimit], que sigue siendo el limite real: este filtro
/// solo actua cuando el cliente declara Content-Length (los navegadores siempre
/// lo hacen al subir un archivo).
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class LimitarTamanoPeticionAttribute : Attribute, IResourceFilter
{
    public LimitarTamanoPeticionAttribute(long maximoBytes, string mensaje)
    {
        MaximoBytes = maximoBytes;
        Mensaje = mensaje;
    }

    public long MaximoBytes { get; }

    public string Mensaje { get; }

    public void OnResourceExecuting(ResourceExecutingContext context)
    {
        var tamano = context.HttpContext.Request.ContentLength;

        if (tamano is null || tamano <= MaximoBytes)
        {
            return;
        }

        var peticion = context.HttpContext.Request;

        context.Result = new ObjectResult(new ProblemDetails
        {
            Status = StatusCodes.Status413PayloadTooLarge,
            Title = "Archivo demasiado grande",
            Detail = Mensaje,
            Instance = $"{peticion.Method} {peticion.Path}",
        })
        {
            StatusCode = StatusCodes.Status413PayloadTooLarge,
            ContentTypes = { "application/problem+json" },
        };
    }

    public void OnResourceExecuted(ResourceExecutedContext context)
    {
    }
}
