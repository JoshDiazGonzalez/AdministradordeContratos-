namespace Contratos.Api.Extensions;

/// <summary>
/// Mensajes de log generados en tiempo de compilacion (source generator).
/// Evitan el boxing y la construccion del mensaje cuando el nivel esta deshabilitado.
/// </summary>
public static partial class LogMessages
{
    [LoggerMessage(
        EventId = 1000,
        Level = LogLevel.Information,
        Message = "Conexion a base de datos configurada: {ConnectionString}")]
    public static partial void ConexionConfigurada(ILogger logger, string connectionString);
}
