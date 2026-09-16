namespace Contratos.Domain.Entities;

/// <summary>
/// Usuario que puede autenticarse en el sistema.
/// Nunca almacena la contrasena en texto plano, solo su hash.
/// </summary>
public class Usuario
{
    private Usuario()
    {
        Username = string.Empty;
        PasswordHash = string.Empty;
        NombreCompleto = string.Empty;
    }

    public Guid Id { get; private set; }

    public string Username { get; private set; }

    /// <summary>Hash BCrypt. Nunca se expone en respuestas ni en logs.</summary>
    public string PasswordHash { get; private set; }

    public string NombreCompleto { get; private set; }

    public DateTimeOffset FechaCreacion { get; private set; }

    public static Usuario Crear(
        string username,
        string passwordHash,
        string nombreCompleto,
        DateTimeOffset ahoraUtc) => new()
        {
            Id = Guid.CreateVersion7(),
            Username = username.Trim().ToLowerInvariant(),
            PasswordHash = passwordHash,
            NombreCompleto = nombreCompleto.Trim(),
            FechaCreacion = ahoraUtc
        };

    public void CambiarPasswordHash(string passwordHash) => PasswordHash = passwordHash;
}
