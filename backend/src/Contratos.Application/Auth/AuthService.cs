using Contratos.Application.Common;
using Contratos.Domain.Entities;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace Contratos.Application.Auth;

/// <summary>Consulta de usuarios para autenticacion.</summary>
public interface IUsuarioRepository
{
    Task<Usuario?> ObtenerPorUsernameAsync(string username, CancellationToken cancellationToken);
}

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
}

public class AuthService : IAuthService
{
    private readonly IUsuarioRepository _usuarios;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _tokenGenerator;
    private readonly IValidator<LoginRequest> _validator;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUsuarioRepository usuarios,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator tokenGenerator,
        IValidator<LoginRequest> validator,
        ILogger<AuthService> logger)
    {
        _usuarios = usuarios;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
        _validator = validator;
        _logger = logger;
    }

    public async Task<LoginResponse> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var validacion = await _validator.ValidateAsync(request, cancellationToken);
        if (!validacion.IsValid)
        {
            throw new ValidacionException(validacion.ToDictionary());
        }

        var username = request.Username.Trim().ToLowerInvariant();
        var usuario = await _usuarios.ObtenerPorUsernameAsync(username, cancellationToken);

        // Se verifica el hash incluso cuando el usuario no existe, contra un hash
        // ficticio, para que el tiempo de respuesta no revele si el usuario es valido
        // (ataque de enumeracion por temporizacion).
        var hashAComparar = usuario?.PasswordHash ?? HashFicticio;
        var passwordCorrecta = _passwordHasher.Verificar(request.Password, hashAComparar);

        if (usuario is null || !passwordCorrecta)
        {
            // Se registra el intento fallido, nunca la contrasena.
            LogAutenticacion.IntentoFallido(_logger, username);
            throw new CredencialesInvalidasException();
        }

        var (token, expiraEn) = _tokenGenerator.Generar(usuario);
        LogAutenticacion.LoginCorrecto(_logger, usuario.Username);

        return new LoginResponse(
            token,
            expiraEn,
            new UsuarioAutenticado(usuario.Username, usuario.NombreCompleto));
    }

    /// <summary>
    /// Hash BCrypt valido pero de una contrasena aleatoria. Solo sirve para gastar
    /// el mismo tiempo de CPU cuando el usuario no existe.
    /// </summary>
    private const string HashFicticio =
        "$2a$11$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy";
}

internal static partial class LogAutenticacion
{
    [LoggerMessage(
        EventId = 2000,
        Level = LogLevel.Warning,
        Message = "Intento de autenticacion fallido para el usuario {Username}")]
    public static partial void IntentoFallido(ILogger logger, string username);

    [LoggerMessage(
        EventId = 2001,
        Level = LogLevel.Information,
        Message = "Autenticacion correcta para el usuario {Username}")]
    public static partial void LoginCorrecto(ILogger logger, string username);
}
