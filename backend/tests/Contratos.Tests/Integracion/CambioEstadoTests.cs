using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Contratos.Tests.Integracion;

/// <summary>
/// Activacion y desactivacion de contratos por decision de negocio.
/// </summary>
public class CambioEstadoTests : IClassFixture<ContratosApiFactory>
{
    private readonly ContratosApiFactory _factory;

    public CambioEstadoTests(ContratosApiFactory factory) => _factory = factory;

    private sealed record ContratoRespuesta(
        Guid Id, string NombreProveedor, string Estado, DateTimeOffset? FechaActualizacion);

    private sealed record Pagina(List<ContratoRespuesta> Items);

    private static async Task<ContratoRespuesta> BuscarAsync(HttpClient cliente, string proveedor)
    {
        var pagina = await cliente.GetFromJsonAsync<Pagina>(
            $"/api/contratos?proveedor={Uri.EscapeDataString(proveedor)}&pageSize=50");
        return pagina!.Items.Single();
    }

    private static Task<HttpResponseMessage> CambiarAsync(HttpClient cliente, Guid id, bool inactivo) =>
        cliente.PatchAsJsonAsync($"/api/contratos/{id}/estado", new { inactivo });

    [Fact]
    public async Task Desactivar_un_contrato_activo_lo_deja_Inactivo()
    {
        var cliente = await _factory.CrearClienteAutenticadoAsync();
        var contrato = await BuscarAsync(cliente, "Proveedor Beta");
        Assert.Equal("Activo", contrato.Estado);

        var respuesta = await CambiarAsync(cliente, contrato.Id, inactivo: true);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var actualizado = await respuesta.Content.ReadFromJsonAsync<ContratoRespuesta>();
        Assert.Equal("Inactivo", actualizado!.Estado);
        Assert.NotNull(actualizado.FechaActualizacion);

        // Deja el dato como estaba para no afectar a otros tests.
        await CambiarAsync(cliente, contrato.Id, inactivo: false);
    }

    [Fact]
    public async Task Reactivar_un_contrato_vencido_devuelve_Vencido_no_Activo()
    {
        // "Proveedor Eta" esta inactivo y su vencimiento ya paso. Reactivarlo no
        // puede convertirlo en vigente: el estado vuelve a calcularse con las fechas.
        var cliente = await _factory.CrearClienteAutenticadoAsync();
        var contrato = await BuscarAsync(cliente, "Proveedor Eta");
        Assert.Equal("Inactivo", contrato.Estado);

        var respuesta = await CambiarAsync(cliente, contrato.Id, inactivo: false);
        var actualizado = await respuesta.Content.ReadFromJsonAsync<ContratoRespuesta>();

        Assert.Equal("Vencido", actualizado!.Estado);

        await CambiarAsync(cliente, contrato.Id, inactivo: true);
    }

    [Fact]
    public async Task Repetir_la_misma_operacion_no_modifica_la_fecha_de_actualizacion()
    {
        var cliente = await _factory.CrearClienteAutenticadoAsync();
        var contrato = await BuscarAsync(cliente, "Proveedor Gamma");

        var primera = await (await CambiarAsync(cliente, contrato.Id, inactivo: true))
            .Content.ReadFromJsonAsync<ContratoRespuesta>();
        var segunda = await (await CambiarAsync(cliente, contrato.Id, inactivo: true))
            .Content.ReadFromJsonAsync<ContratoRespuesta>();

        Assert.Equal(primera!.FechaActualizacion, segunda!.FechaActualizacion);

        await CambiarAsync(cliente, contrato.Id, inactivo: false);
    }

    [Fact]
    public async Task El_cambio_persiste_y_se_refleja_en_el_listado()
    {
        var cliente = await _factory.CrearClienteAutenticadoAsync();
        var contrato = await BuscarAsync(cliente, "Proveedor Delta");

        await CambiarAsync(cliente, contrato.Id, inactivo: true);
        var releido = await cliente.GetFromJsonAsync<ContratoRespuesta>($"/api/contratos/{contrato.Id}");

        Assert.Equal("Inactivo", releido!.Estado);

        await CambiarAsync(cliente, contrato.Id, inactivo: false);
    }

    [Fact]
    public async Task No_se_puede_fijar_un_estado_calculado()
    {
        // El cuerpo solo admite "inactivo". Un intento de fijar "estado": "Activo"
        // se ignora: no hay forma de marcar como vigente un contrato vencido.
        var cliente = await _factory.CrearClienteAutenticadoAsync();
        var contrato = await BuscarAsync(cliente, "Proveedor Zeta");

        var respuesta = await cliente.PatchAsJsonAsync(
            $"/api/contratos/{contrato.Id}/estado", new { estado = "Activo", inactivo = false });
        var actualizado = await respuesta.Content.ReadFromJsonAsync<ContratoRespuesta>();

        Assert.Equal("Vencido", actualizado!.Estado);
    }

    [Fact]
    public async Task Un_contrato_inexistente_devuelve_404()
    {
        var cliente = await _factory.CrearClienteAutenticadoAsync();

        var respuesta = await CambiarAsync(cliente, Guid.CreateVersion7(), inactivo: true);

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }

    [Fact]
    public async Task Sin_token_devuelve_401()
    {
        var respuesta = await _factory.CreateClient()
            .PatchAsJsonAsync($"/api/contratos/{Guid.CreateVersion7()}/estado", new { inactivo = true });

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    public async Task El_documento_se_sirve_sin_cache_y_sin_deduccion_de_tipo()
    {
        var cliente = await _factory.CrearClienteAutenticadoAsync();

        // Se crea un contrato real: los contratos sembrados para estos tests no
        // tienen su documento guardado en el almacenamiento.
        using var formulario = new MultipartFormDataContent
        {
            { new StringContent("Proveedor Cabeceras"), "NombreProveedor" },
            { new StringContent("100"), "MontoContrato" },
            { new StringContent("2026-01-01"), "FechaInicio" },
            { new StringContent("2027-12-31"), "FechaVencimiento" },
            { new StringContent("Prueba de cabeceras."), "Descripcion" },
        };
        var documento = new ByteArrayContent("%PDF-1.7 prueba"u8.ToArray());
        documento.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
        formulario.Add(documento, "Archivo", "cabeceras.pdf");

        var creado = await (await cliente.PostAsync("/api/contratos", formulario))
            .Content.ReadFromJsonAsync<ContratoRespuesta>();

        var respuesta = await cliente.GetAsync($"/api/contratos/{creado!.Id}/archivo");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.True(respuesta.Headers.CacheControl?.NoStore);
        Assert.Contains("nosniff", respuesta.Headers.GetValues("X-Content-Type-Options"));
    }

    [Fact]
    public async Task Las_respuestas_de_error_tambien_llevan_nosniff()
    {
        // Regresion: el middleware de errores limpiaba la respuesta y la cabecera
        // desaparecia de todos los 4xx y 5xx.
        var cliente = await _factory.CrearClienteAutenticadoAsync();

        var respuesta = await cliente.GetAsync($"/api/contratos/{Guid.CreateVersion7()}");

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        Assert.Contains("nosniff", respuesta.Headers.GetValues("X-Content-Type-Options"));
    }
}
