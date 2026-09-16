namespace Contratos.Api.Extensions;

/// <summary>
/// Utilidades de configuracion de la API.
/// </summary>
public static class ConfiguracionExtensions
{
    /// <summary>
    /// Oculta la contrasena de una cadena de conexion para poder registrarla en logs.
    /// </summary>
    public static string Ofuscar(this string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return "(vacia)";
        }

        var partes = connectionString
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(parte =>
            {
                var separador = parte.IndexOf('=');
                if (separador <= 0)
                {
                    return parte;
                }

                var clave = parte[..separador];
                return clave.Equals("Password", StringComparison.OrdinalIgnoreCase)
                    ? $"{clave}=***"
                    : parte;
            });

        return string.Join(';', partes);
    }
}
