using System.Net;
using System.Text.Json;

namespace Contratos.Tests.Integracion;

/// <summary>
/// Parametros de consulta con formato invalido (fechas imposibles, estados
/// inexistentes). Los detecta ASP.NET antes de llegar al servicio.
/// </summary>
public class ValidacionParametrosTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public ValidacionParametrosTests(ApiFactory factory) => _factory = factory;

    [Theory]
    [InlineData("fechaInicioDesde=2026-02-30", "FechaInicioDesde")]
    [InlineData("fechaVencimientoHasta=no-es-fecha", "FechaVencimientoHasta")]
    [InlineData("estado=Inexistente", "Estado")]
    [InlineData("page=abc", "page")]
    public async Task Un_parametro_mal_formado_devuelve_400_en_espanol(string query, string campo)
    {
        var cliente = await _factory.CrearClienteAutenticadoAsync();

        var respuesta = await cliente.GetAsync($"/api/contratos?{query}");

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal("application/problem+json", respuesta.Content.Headers.ContentType?.MediaType);

        using var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        var raiz = json.RootElement;

        // Mismo titulo que los errores de validacion de negocio.
        Assert.Equal("Error de validacion", raiz.GetProperty("title").GetString());

        var errores = raiz.GetProperty("errors");
        var mensajes = errores.EnumerateObject()
            .Where(p => p.Name.Equals(campo, StringComparison.OrdinalIgnoreCase))
            .SelectMany(p => p.Value.EnumerateArray().Select(m => m.GetString() ?? string.Empty))
            .ToList();

        Assert.NotEmpty(mensajes);
        Assert.All(mensajes, m => Assert.DoesNotContain("is not valid", m, StringComparison.Ordinal));
        Assert.Contains(mensajes, m => m.Contains("no es válido", StringComparison.Ordinal));
    }
}
