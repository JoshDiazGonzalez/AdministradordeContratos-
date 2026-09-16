using System.Net.Mime;
using Contratos.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace Contratos.Api.Middleware;

/// <summary>
/// Traduce las excepciones de la aplicacion a respuestas ProblemDetails con el
/// codigo HTTP adecuado. Es el unico punto donde se decide que ve el cliente
/// cuando algo falla, para no repetir try/catch en cada controlador.
///
/// Nunca expone el stack trace fuera de Development.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _entorno;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment entorno)
    {
        _next = next;
        _logger = logger;
        _entorno = entorno;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await EscribirRespuestaAsync(context, ex);
        }
    }

    private async Task EscribirRespuestaAsync(HttpContext context, Exception ex)
    {
        if (context.Response.HasStarted)
        {
            // La respuesta ya viaja al cliente: no se puede reescribir la cabecera.
            LogErrores.RespuestaYaIniciada(_logger, ex);
            throw ex;
        }

        var problema = Traducir(ex, context);

        context.Response.Clear();
        context.Response.StatusCode = problema.Status ?? StatusCodes.Status500InternalServerError;

        // Se serializa con el tipo en tiempo de ejecucion: si se usara el tipo
        // estatico ProblemDetails, un ValidationProblemDetails perderia su
        // propiedad "errors" y el cliente no veria el detalle por campo.
        // El contentType se pasa explicitamente porque WriteAsJsonAsync lo
        // sobrescribiria a "application/json".
        await context.Response.WriteAsJsonAsync(
            problema,
            problema.GetType(),
            options: null,
            contentType: MediaTypeNames.Application.ProblemJson);
    }

    private ProblemDetails Traducir(Exception ex, HttpContext context)
    {
        var instancia = $"{context.Request.Method} {context.Request.Path}";

        switch (ex)
        {
            case ValidacionException validacion:
                LogErrores.Validacion(_logger, instancia);
                return new ValidationProblemDetails(validacion.Errores)
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Error de validacion",
                    Instance = instancia
                };

            case CredencialesInvalidasException:
                // Se registra como advertencia, sin datos sensibles.
                LogErrores.NoAutorizado(_logger, instancia);
                return new ProblemDetails
                {
                    Status = StatusCodes.Status401Unauthorized,
                    Title = "No autorizado",
                    Detail = ex.Message,
                    Instance = instancia
                };

            case RecursoNoEncontradoException:
                return new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Recurso no encontrado",
                    Detail = ex.Message,
                    Instance = instancia
                };

            case ArgumentOutOfRangeException or ArgumentException:
                return new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Solicitud invalida",
                    Detail = ex.Message,
                    Instance = instancia
                };

            default:
                LogErrores.NoControlado(_logger, ex);
                return new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "Error interno del servidor",
                    // El detalle tecnico solo se revela en desarrollo.
                    Detail = _entorno.IsDevelopment()
                        ? ex.ToString()
                        : "Ocurrio un error inesperado. Contacte al administrador.",
                    Instance = instancia
                };
        }
    }
}

internal static partial class LogErrores
{
    [LoggerMessage(
        EventId = 4000,
        Level = LogLevel.Error,
        Message = "Excepcion no controlada")]
    public static partial void NoControlado(ILogger logger, Exception ex);

    [LoggerMessage(
        EventId = 4001,
        Level = LogLevel.Warning,
        Message = "Acceso no autorizado en {Instancia}")]
    public static partial void NoAutorizado(ILogger logger, string instancia);

    [LoggerMessage(
        EventId = 4002,
        Level = LogLevel.Information,
        Message = "Solicitud invalida en {Instancia}")]
    public static partial void Validacion(ILogger logger, string instancia);

    [LoggerMessage(
        EventId = 4003,
        Level = LogLevel.Error,
        Message = "La respuesta ya habia comenzado; no se pudo escribir ProblemDetails")]
    public static partial void RespuestaYaIniciada(ILogger logger, Exception ex);
}
