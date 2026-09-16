using Contratos.Application.Auth;

namespace Contratos.Infrastructure.Auth;

/// <summary>
/// Hashing con BCrypt. El salt se genera por contrasena y viaja dentro del hash,
/// por eso no hace falta una columna aparte.
/// </summary>
public class BCryptPasswordHasher : IPasswordHasher
{
    /// <summary>
    /// Factor de trabajo. 11 supone ~100 ms por verificacion en hardware actual:
    /// suficiente para frenar la fuerza bruta sin degradar el login.
    /// </summary>
    private const int WorkFactor = 11;

    public string Hash(string password) =>
        BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

    public bool Verificar(string password, string hash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            // Hash con formato invalido: se trata como credencial incorrecta,
            // nunca como error del servidor.
            return false;
        }
    }
}
