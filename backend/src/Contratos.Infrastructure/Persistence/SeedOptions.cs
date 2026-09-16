namespace Contratos.Infrastructure.Persistence;

/// <summary>
/// Datos iniciales. La contrasena del administrador llega por variable de entorno.
/// </summary>
public class SeedOptions
{
    public const string SectionName = "Seed";

    public string AdminUsername { get; set; } = "admin";

    public string AdminPassword { get; set; } = string.Empty;

    public string AdminFullName { get; set; } = "Administrador";

    /// <summary>
    /// Crea contratos ficticios que cubren los cuatro estados, para demostrar la
    /// aplicacion sin cargar datos a mano. Desactivado por defecto: nunca debe
    /// ejecutarse contra una base con datos reales.
    /// </summary>
    public bool DatosDemo { get; set; }
}
