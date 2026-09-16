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

    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Information,
        Message = "Variables de entorno cargadas desde {Ruta}")]
    public static partial void ArchivoEnvCargado(ILogger logger, string ruta);

    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Warning,
        Message = "Sin PostgreSQL configurado: se usa la base local de desarrollo en {Ruta}. " +
                  "Defina ConnectionStrings__DefaultConnection para conectar con Supabase.")]
    public static partial void BaseLocalEnUso(ILogger logger, string ruta);

    [LoggerMessage(
        EventId = 1003,
        Level = LogLevel.Information,
        Message = "Esquema de base de datos actualizado con las migraciones pendientes.")]
    public static partial void MigracionesAplicadas(ILogger logger);
}
