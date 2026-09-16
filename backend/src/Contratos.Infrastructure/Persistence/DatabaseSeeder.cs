using Contratos.Application.Auth;
using Contratos.Domain.Entities;
using Contratos.Domain.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Contratos.Infrastructure.Persistence;

/// <summary>
/// Crea el usuario administrador inicial si todavia no existe.
/// Es idempotente: se puede ejecutar en cada arranque sin duplicar datos.
/// </summary>
public class DatabaseSeeder
{
    private readonly AppDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IDateTimeProvider _reloj;
    private readonly SeedOptions _options;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(
        AppDbContext context,
        IPasswordHasher passwordHasher,
        IDateTimeProvider reloj,
        IOptions<SeedOptions> options,
        ILogger<DatabaseSeeder> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _reloj = reloj;
        _options = options.Value;
        _logger = logger;
    }

    public async Task SembrarAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.AdminPassword))
        {
            LogSeed.SinPassword(_logger);
            return;
        }

        var username = _options.AdminUsername.Trim().ToLowerInvariant();

        var yaExiste = await _context.Usuarios
            .AnyAsync(u => u.Username == username, cancellationToken);

        if (yaExiste)
        {
            LogSeed.UsuarioYaExiste(_logger, username);
            return;
        }

        var usuario = Usuario.Crear(
            username,
            _passwordHasher.Hash(_options.AdminPassword),
            _options.AdminFullName,
            _reloj.AhoraUtc);

        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync(cancellationToken);

        LogSeed.UsuarioCreado(_logger, username);
    }
}

internal static partial class LogSeed
{
    [LoggerMessage(
        EventId = 3000,
        Level = LogLevel.Warning,
        Message = "Seed omitido: no se definio Seed__AdminPassword. No habra usuario inicial.")]
    public static partial void SinPassword(ILogger logger);

    [LoggerMessage(
        EventId = 3001,
        Level = LogLevel.Information,
        Message = "Seed omitido: el usuario {Username} ya existe.")]
    public static partial void UsuarioYaExiste(ILogger logger, string username);

    [LoggerMessage(
        EventId = 3002,
        Level = LogLevel.Information,
        Message = "Usuario administrador {Username} creado.")]
    public static partial void UsuarioCreado(ILogger logger, string username);
}
