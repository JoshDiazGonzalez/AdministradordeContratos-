using Contratos.Infrastructure.Auth;

namespace Contratos.Tests.Auth;

public class BCryptPasswordHasherTests
{
    private readonly BCryptPasswordHasher _hasher = new();

    [Fact]
    public void El_hash_no_contiene_la_contrasena_en_claro()
    {
        var hash = _hasher.Hash("Admin123*");

        Assert.DoesNotContain("Admin123*", hash, StringComparison.Ordinal);
        Assert.StartsWith("$2", hash, StringComparison.Ordinal);
    }

    [Fact]
    public void Dos_hashes_de_la_misma_contrasena_son_distintos()
    {
        // El salt es aleatorio por hash: dos usuarios con la misma contrasena
        // no comparten hash, asi que una tabla arcoiris no sirve.
        var a = _hasher.Hash("Admin123*");
        var b = _hasher.Hash("Admin123*");

        Assert.NotEqual(a, b);
        Assert.True(_hasher.Verificar("Admin123*", a));
        Assert.True(_hasher.Verificar("Admin123*", b));
    }

    [Fact]
    public void Verificar_rechaza_una_contrasena_incorrecta()
    {
        var hash = _hasher.Hash("Admin123*");

        Assert.False(_hasher.Verificar("admin123*", hash));
        Assert.False(_hasher.Verificar("", hash));
    }

    [Fact]
    public void Verificar_no_lanza_ante_un_hash_con_formato_invalido()
    {
        // Un hash corrupto en base de datos debe traducirse a credencial
        // incorrecta, nunca a un error 500.
        Assert.False(_hasher.Verificar("cualquiera", "esto-no-es-un-hash"));
    }
}
