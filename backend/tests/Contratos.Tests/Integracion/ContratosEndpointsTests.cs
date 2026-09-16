using System.Net;
using System.Net.Http.Json;
using Contratos.Domain.Entities;
using Contratos.Infrastructure.Persistence;

namespace Contratos.Tests.Integracion;

/// <summary>
/// Listado de contratos: filtros, paginacion y traduccion del estado a SQL.
/// Se usa una factory propia para no compartir datos con los tests de auth.
/// </summary>
public class ContratosEndpointsTests : IClassFixture<ContratosApiFactory>
{
    private readonly ContratosApiFactory _factory;

    public ContratosEndpointsTests(ContratosApiFactory factory) => _factory = factory;

    private sealed record Pagina(
        List<ContratoResumen> Items, int Page, int PageSize, int TotalItems, int TotalPages);

    private sealed record ContratoResumen(
        Guid Id, string NombreProveedor, decimal MontoContrato, string Estado);

    [Fact]
    public async Task Sin_token_devuelve_401()
    {
        var respuesta = await _factory.CreateClient().GetAsync("/api/contratos");

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    public async Task Listado_sin_filtros_devuelve_todos_los_contratos_paginados()
    {
        var cliente = await _factory.CrearClienteAutenticadoAsync();

        var pagina = await cliente.GetFromJsonAsync<Pagina>("/api/contratos?pageSize=50");

        Assert.NotNull(pagina);
        Assert.Equal(ContratosApiFactory.TotalContratos, pagina!.TotalItems);
        Assert.Equal(ContratosApiFactory.TotalContratos, pagina.Items.Count);
    }

    [Theory]
    [InlineData("Activo", 2)]
    [InlineData("PorVencer", 2)]
    [InlineData("Vencido", 2)]
    [InlineData("Inactivo", 1)]
    public async Task Filtro_por_estado_devuelve_solo_ese_estado(string estado, int esperados)
    {
        var cliente = await _factory.CrearClienteAutenticadoAsync();

        var pagina = await cliente.GetFromJsonAsync<Pagina>(
            $"/api/contratos?estado={estado}&pageSize=50");

        Assert.NotNull(pagina);
        Assert.Equal(esperados, pagina!.TotalItems);
        Assert.All(pagina.Items, c => Assert.Equal(estado, c.Estado));
    }

    [Fact]
    public async Task El_estado_viaja_como_texto_no_como_numero()
    {
        var cliente = await _factory.CrearClienteAutenticadoAsync();

        var texto = await cliente.GetStringAsync("/api/contratos?pageSize=50");

        Assert.Contains("\"estado\":\"", texto, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Inactivo_prevalece_aunque_las_fechas_digan_vencido()
    {
        var cliente = await _factory.CrearClienteAutenticadoAsync();

        var inactivos = await cliente.GetFromJsonAsync<Pagina>("/api/contratos?estado=Inactivo");
        var vencidos = await cliente.GetFromJsonAsync<Pagina>("/api/contratos?estado=Vencido&pageSize=50");

        // El contrato inactivo tiene vencimiento pasado, pero no aparece como vencido.
        var idInactivo = inactivos!.Items.Single().Id;
        Assert.DoesNotContain(vencidos!.Items, c => c.Id == idInactivo);
    }

    [Theory]
    [InlineData("alpha", 1)]
    [InlineData("ALPHA", 1)]
    [InlineData("prov", ContratosApiFactory.TotalContratos)]
    [InlineData("inexistente", 0)]
    public async Task Filtro_de_proveedor_es_parcial_y_no_distingue_mayusculas(
        string termino, int esperados)
    {
        var cliente = await _factory.CrearClienteAutenticadoAsync();

        var pagina = await cliente.GetFromJsonAsync<Pagina>(
            $"/api/contratos?proveedor={termino}&pageSize=50");

        Assert.Equal(esperados, pagina!.TotalItems);
    }

    [Theory]
    [InlineData("epsilon")]
    [InlineData("EPSILON")]
    [InlineData("épsilon")]
    [InlineData("  EpSíLoN  ")]
    public async Task El_filtro_de_proveedor_ignora_tildes_mayusculas_y_espacios(string termino)
    {
        // "Proveedor Epsilon" se encuentra escriba el usuario como escriba.
        var cliente = await _factory.CrearClienteAutenticadoAsync();

        var pagina = await cliente.GetFromJsonAsync<Pagina>(
            $"/api/contratos?proveedor={Uri.EscapeDataString(termino)}&pageSize=50");

        Assert.Single(pagina!.Items);
        Assert.Equal("Proveedor Epsilon", pagina.Items[0].NombreProveedor);
    }

    [Fact]
    public async Task Los_filtros_se_combinan()
    {
        var cliente = await _factory.CrearClienteAutenticadoAsync();

        var pagina = await cliente.GetFromJsonAsync<Pagina>(
            "/api/contratos?proveedor=prov&estado=Vencido&pageSize=50");

        Assert.Equal(2, pagina!.TotalItems);
        Assert.All(pagina.Items, c => Assert.Equal("Vencido", c.Estado));
    }

    [Fact]
    public async Task La_paginacion_no_repite_ni_pierde_filas()
    {
        var cliente = await _factory.CrearClienteAutenticadoAsync();

        var pagina1 = await cliente.GetFromJsonAsync<Pagina>("/api/contratos?page=1&pageSize=3");
        var pagina2 = await cliente.GetFromJsonAsync<Pagina>("/api/contratos?page=2&pageSize=3");

        Assert.Equal(3, pagina1!.Items.Count);
        Assert.Equal(ContratosApiFactory.TotalContratos, pagina1.TotalItems);
        Assert.Equal(3, pagina1.TotalPages);

        var ids1 = pagina1.Items.Select(c => c.Id).ToList();
        var ids2 = pagina2!.Items.Select(c => c.Id).ToList();
        Assert.Empty(ids1.Intersect(ids2));
    }

    [Fact]
    public async Task El_total_refleja_el_filtro_no_la_tabla_completa()
    {
        var cliente = await _factory.CrearClienteAutenticadoAsync();

        // Con pageSize=1 solo vuelve una fila, pero totalItems debe ser el total
        // filtrado: si se filtrara en memoria, este numero saldria mal.
        var pagina = await cliente.GetFromJsonAsync<Pagina>(
            "/api/contratos?estado=Vencido&page=1&pageSize=1");

        Assert.Single(pagina!.Items);
        Assert.Equal(2, pagina.TotalItems);
        Assert.Equal(2, pagina.TotalPages);
    }

    [Theory]
    [InlineData("page=0")]
    [InlineData("pageSize=0")]
    [InlineData("pageSize=51")]
    [InlineData("fechaInicioDesde=2026-12-31&fechaInicioHasta=2026-01-01")]
    public async Task Filtros_invalidos_devuelven_400(string query)
    {
        var cliente = await _factory.CrearClienteAutenticadoAsync();

        var respuesta = await cliente.GetAsync($"/api/contratos?{query}");

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    public async Task Obtener_por_id_inexistente_devuelve_404()
    {
        var cliente = await _factory.CrearClienteAutenticadoAsync();

        var respuesta = await cliente.GetAsync($"/api/contratos/{Guid.CreateVersion7()}");

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        Assert.Equal("application/problem+json",
            respuesta.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Obtener_por_id_existente_devuelve_el_contrato()
    {
        var cliente = await _factory.CrearClienteAutenticadoAsync();
        var pagina = await cliente.GetFromJsonAsync<Pagina>("/api/contratos?pageSize=1");
        var id = pagina!.Items.Single().Id;

        var contrato = await cliente.GetFromJsonAsync<ContratoResumen>($"/api/contratos/{id}");

        Assert.Equal(id, contrato!.Id);
    }

    [Fact]
    public async Task El_resumen_cuadra_con_el_total()
    {
        var cliente = await _factory.CrearClienteAutenticadoAsync();

        var resumen = await cliente.GetFromJsonAsync<Dictionary<string, int>>(
            "/api/contratos/resumen");

        Assert.Equal(ContratosApiFactory.TotalContratos, resumen!["total"]);
        Assert.Equal(2, resumen["activos"]);
        Assert.Equal(2, resumen["porVencer"]);
        Assert.Equal(2, resumen["vencidos"]);
        Assert.Equal(1, resumen["inactivos"]);
        Assert.Equal(
            resumen["total"],
            resumen["activos"] + resumen["porVencer"] + resumen["vencidos"] + resumen["inactivos"]);
    }
}
