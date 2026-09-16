using System.IdentityModel.Tokens.Jwt;
using Contratos.Domain.Entities;
using Contratos.Domain.Services;
using Contratos.Infrastructure.Auth;
using Microsoft.Extensions.Options;

namespace Contratos.Tests.Auth;

public class JwtTokenGeneratorTests
{
    private sealed class RelojFijo : IDateTimeProvider
    {
        public DateOnly Hoy => new(2026, 6, 15);

        public DateTimeOffset AhoraUtc => new(2026, 6, 15, 10, 0, 0, TimeSpan.Zero);
    }

    private static readonly JwtOptions Opciones = new()
    {
        Secret = "clave-de-pruebas-con-mas-de-32-caracteres!!",
        Issuer = "ContratosApi",
        Audience = "ContratosApp",
        ExpirationMinutes = 60
    };

    private static JwtTokenGenerator CrearGenerador() =>
        new(Options.Create(Opciones), new RelojFijo());

    private static Usuario CrearUsuario() =>
        Usuario.Crear("admin", "hash", "Administrador", new RelojFijo().AhoraUtc);

    [Fact]
    public void El_token_lleva_issuer_audience_y_expiracion_configurados()
    {
        var usuario = CrearUsuario();

        var (token, expiraEn) = CrearGenerador().Generar(usuario);
        var leido = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal("ContratosApi", leido.Issuer);
        Assert.Contains("ContratosApp", leido.Audiences);
        Assert.Equal(new RelojFijo().AhoraUtc.AddMinutes(60), expiraEn);
    }

    [Fact]
    public void El_token_identifica_al_usuario_sin_exponer_su_hash()
    {
        var usuario = CrearUsuario();

        var (token, _) = CrearGenerador().Generar(usuario);
        var leido = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal(usuario.Id.ToString(), leido.Subject);
        Assert.DoesNotContain("hash", token, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Cada_token_tiene_un_identificador_unico()
    {
        var usuario = CrearUsuario();
        var generador = CrearGenerador();

        var jtis = Enumerable.Range(0, 5)
            .Select(_ => new JwtSecurityTokenHandler().ReadJwtToken(generador.Generar(usuario).Token).Id)
            .ToList();

        Assert.Equal(5, jtis.Distinct().Count());
    }
}
