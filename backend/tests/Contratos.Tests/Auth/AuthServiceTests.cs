using Contratos.Application.Auth;
using Contratos.Application.Common;
using Contratos.Domain.Entities;
using Contratos.Domain.Services;
using Contratos.Infrastructure.Auth;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Contratos.Tests.Auth;

public class AuthServiceTests
{
    private const string PasswordValida = "Admin123*";

    private sealed class RelojFijo : IDateTimeProvider
    {
        public DateOnly Hoy => new(2026, 6, 15);

        public DateTimeOffset AhoraUtc => new(2026, 6, 15, 10, 0, 0, TimeSpan.Zero);
    }

    private sealed class RepositorioFalso : IUsuarioRepository
    {
        private readonly Usuario? _usuario;

        public RepositorioFalso(Usuario? usuario) => _usuario = usuario;

        public string? UltimoUsernameConsultado { get; private set; }

        public Task<Usuario?> ObtenerPorUsernameAsync(string username, CancellationToken _)
        {
            UltimoUsernameConsultado = username;
            return Task.FromResult(_usuario?.Username == username ? _usuario : null);
        }
    }

    private static readonly BCryptPasswordHasher Hasher = new();

    private static Usuario UsuarioAdmin() => Usuario.Crear(
        "admin", Hasher.Hash(PasswordValida), "Administrador", new RelojFijo().AhoraUtc);

    private static (AuthService Servicio, RepositorioFalso Repositorio) Crear(Usuario? usuario)
    {
        var repositorio = new RepositorioFalso(usuario);
        var opciones = Options.Create(new JwtOptions
        {
            Secret = "clave-de-pruebas-con-mas-de-32-caracteres!!",
            ExpirationMinutes = 60
        });

        var servicio = new AuthService(
            repositorio,
            Hasher,
            new JwtTokenGenerator(opciones, new RelojFijo()),
            new LoginRequestValidator(),
            NullLogger<AuthService>.Instance);

        return (servicio, repositorio);
    }

    [Fact]
    public async Task Credenciales_correctas_devuelven_token_y_usuario()
    {
        var (servicio, _) = Crear(UsuarioAdmin());

        var respuesta = await servicio.LoginAsync(
            new LoginRequest("admin", PasswordValida), CancellationToken.None);

        Assert.False(string.IsNullOrWhiteSpace(respuesta.Token));
        Assert.Equal("admin", respuesta.User.Username);
        Assert.Equal("Administrador", respuesta.User.NombreCompleto);
        Assert.True(respuesta.ExpiresAt > new RelojFijo().AhoraUtc);
    }

    [Fact]
    public async Task La_respuesta_no_expone_el_hash_de_la_contrasena()
    {
        var (servicio, _) = Crear(UsuarioAdmin());

        var respuesta = await servicio.LoginAsync(
            new LoginRequest("admin", PasswordValida), CancellationToken.None);

        var serializada = System.Text.Json.JsonSerializer.Serialize(respuesta);
        Assert.DoesNotContain("$2", serializada, StringComparison.Ordinal);
        Assert.DoesNotContain(PasswordValida, serializada, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Password_incorrecta_lanza_CredencialesInvalidas()
    {
        var (servicio, _) = Crear(UsuarioAdmin());

        await Assert.ThrowsAsync<CredencialesInvalidasException>(() =>
            servicio.LoginAsync(new LoginRequest("admin", "incorrecta"), CancellationToken.None));
    }

    [Fact]
    public async Task Usuario_inexistente_lanza_la_misma_excepcion_que_password_incorrecta()
    {
        // El mensaje no debe permitir distinguir si el usuario existe.
        var (servicio, _) = Crear(UsuarioAdmin());

        var porUsuario = await Assert.ThrowsAsync<CredencialesInvalidasException>(() =>
            servicio.LoginAsync(new LoginRequest("fantasma", PasswordValida), CancellationToken.None));

        var porPassword = await Assert.ThrowsAsync<CredencialesInvalidasException>(() =>
            servicio.LoginAsync(new LoginRequest("admin", "incorrecta"), CancellationToken.None));

        Assert.Equal(porPassword.Message, porUsuario.Message);
    }

    [Fact]
    public async Task El_username_se_normaliza_a_minusculas()
    {
        var (servicio, repositorio) = Crear(UsuarioAdmin());

        var respuesta = await servicio.LoginAsync(
            new LoginRequest("  ADMIN  ", PasswordValida), CancellationToken.None);

        Assert.Equal("admin", repositorio.UltimoUsernameConsultado);
        Assert.Equal("admin", respuesta.User.Username);
    }

    [Theory]
    [InlineData("", "Admin123*")]
    [InlineData("admin", "")]
    [InlineData("   ", "   ")]
    public async Task Entrada_vacia_lanza_ValidacionException(string username, string password)
    {
        var (servicio, _) = Crear(UsuarioAdmin());

        var ex = await Assert.ThrowsAsync<ValidacionException>(() =>
            servicio.LoginAsync(new LoginRequest(username, password), CancellationToken.None));

        Assert.NotEmpty(ex.Errores);
    }
}
