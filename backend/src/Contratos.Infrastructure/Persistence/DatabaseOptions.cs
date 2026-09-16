namespace Contratos.Infrastructure.Persistence;

/// <summary>
/// Opciones de acceso a datos. Se enlazan desde la seccion "Database"
/// y pueden sobrescribirse por variables de entorno (Database__CommandTimeoutSeconds).
/// </summary>
public class DatabaseOptions
{
    public const string SectionName = "Database";

    /// <summary>Segundos antes de abortar un comando SQL.</summary>
    public int CommandTimeoutSeconds { get; set; } = 30;

    /// <summary>Reintentos ante fallos transitorios de red hacia Supabase.</summary>
    public int MaxRetryCount { get; set; } = 3;

    /// <summary>Espera maxima entre reintentos, en segundos.</summary>
    public int MaxRetryDelaySeconds { get; set; } = 10;

    /// <summary>Registra los parametros de las consultas en el log. Solo para desarrollo.</summary>
    public bool EnableSensitiveDataLogging { get; set; }
}
