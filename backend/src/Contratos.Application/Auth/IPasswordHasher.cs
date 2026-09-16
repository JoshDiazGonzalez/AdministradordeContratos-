namespace Contratos.Application.Auth;

/// <summary>
/// Abstrae el algoritmo de hashing para no acoplar la logica de negocio a BCrypt.
/// </summary>
public interface IPasswordHasher
{
    string Hash(string password);

    bool Verificar(string password, string hash);
}
