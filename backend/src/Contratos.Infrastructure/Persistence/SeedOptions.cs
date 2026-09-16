namespace Contratos.Infrastructure.Persistence;

/// <summary>Usuario administrador inicial. La contrasena llega por variable de entorno.</summary>
public class SeedOptions
{
    public const string SectionName = "Seed";

    public string AdminUsername { get; set; } = "admin";

    public string AdminPassword { get; set; } = string.Empty;

    public string AdminFullName { get; set; } = "Administrador";
}
