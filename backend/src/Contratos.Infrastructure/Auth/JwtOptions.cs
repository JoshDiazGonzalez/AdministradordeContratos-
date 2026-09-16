namespace Contratos.Infrastructure.Auth;

/// <summary>Configuracion del emisor de tokens. Se enlaza desde la seccion "Jwt".</summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>Clave de firma. Debe llegar por variable de entorno, nunca por appsettings.</summary>
    public string Secret { get; set; } = string.Empty;

    public string Issuer { get; set; } = "ContratosApi";

    public string Audience { get; set; } = "ContratosApp";

    public int ExpirationMinutes { get; set; } = 60;

    /// <summary>Longitud minima de la clave para HMAC-SHA256 (256 bits).</summary>
    public const int LongitudMinimaSecret = 32;
}
