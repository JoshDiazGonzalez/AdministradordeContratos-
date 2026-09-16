using Contratos.Domain.Entities;
using Contratos.Domain.Services;

namespace Contratos.Tests.Domain;

public class TextoBusquedaTests
{
    /// <summary>
    /// Casos compartidos con la verificacion del relleno SQL de la migracion
    /// BusquedaProveedorSinTildes: ambas implementaciones deben coincidir.
    /// </summary>
    public static TheoryData<string, string> Casos => new()
    {
        { "Seguridad Integral del Pacífico", "seguridad integral del pacifico" },
        { "Servicios Tecnológicos Omega", "servicios tecnologicos omega" },
        { "  Consultora   Financiera  ", "consultora financiera" },
        { "ÑANDÚ Cía. Ltda.", "nandu cia. ltda." },
        { "Pingüino Àèìòù Ç", "pinguino aeiou c" },
        { "PROVEEDOR ALPHA", "proveedor alpha" },
    };

    [Theory]
    [MemberData(nameof(Casos))]
    public void Normaliza_mayusculas_tildes_y_espacios(string entrada, string esperado)
    {
        Assert.Equal(esperado, TextoBusqueda.Normalizar(entrada));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Un_texto_vacio_queda_vacio(string? entrada)
    {
        Assert.Equal(string.Empty, TextoBusqueda.Normalizar(entrada));
    }

    [Fact]
    public void La_entidad_calcula_el_nombre_de_busqueda_al_crear_y_al_actualizar()
    {
        var ahora = DateTimeOffset.UtcNow;
        var contrato = Contrato.Crear(
            "Seguridad Integral del Pacífico", 100m,
            new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31),
            "Descripcion", "a.pdf", "r/a.pdf", "application/pdf", 10, ahora);

        Assert.Equal("seguridad integral del pacifico", contrato.NombreProveedorBusqueda);

        contrato.Actualizar(
            "Servicios Tecnológicos Omega", 100m,
            new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31),
            "Descripcion", ahora);

        // Nunca puede quedar desincronizado del nombre visible.
        Assert.Equal("servicios tecnologicos omega", contrato.NombreProveedorBusqueda);
    }
}
