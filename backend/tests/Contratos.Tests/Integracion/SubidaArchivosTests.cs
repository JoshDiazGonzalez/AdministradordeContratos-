using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Contratos.Tests.Integracion;

/// <summary>
/// Creacion de contratos con documento adjunto y descarga posterior.
/// </summary>
public class SubidaArchivosTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public SubidaArchivosTests(ApiFactory factory) => _factory = factory;

    private static readonly byte[] ContenidoPdf =
        "%PDF-1.7\nContrato de prueba\n"u8.ToArray();

    private static MultipartFormDataContent Formulario(
        byte[]? contenido = null,
        string nombreArchivo = "contrato.pdf",
        string contentType = "application/pdf",
        string proveedor = "Proveedor Nuevo",
        string monto = "15000.50",
        string? fechaInicio = null,
        string? fechaVencimiento = null,
        bool incluirArchivo = true)
    {
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);

        var form = new MultipartFormDataContent
        {
            { new StringContent(proveedor), "NombreProveedor" },
            { new StringContent(monto), "MontoContrato" },
            { new StringContent(fechaInicio ?? hoy.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)), "FechaInicio" },
            { new StringContent(fechaVencimiento ?? hoy.AddYears(1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
              "FechaVencimiento" },
            { new StringContent("Contrato de servicios."), "Descripcion" }
        };

        if (incluirArchivo)
        {
            var archivo = new ByteArrayContent(contenido ?? ContenidoPdf);
            archivo.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            form.Add(archivo, "Archivo", nombreArchivo);
        }

        return form;
    }

    private static async Task<Guid> CrearContratoAsync(HttpClient cliente)
    {
        var respuesta = await cliente.PostAsync("/api/contratos", Formulario());
        respuesta.EnsureSuccessStatusCode();

        using var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("id").GetGuid();
    }

    [Fact]
    public async Task Crear_sin_token_devuelve_401()
    {
        var respuesta = await _factory.CreateClient()
            .PostAsync("/api/contratos", Formulario());

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    public async Task Crear_con_datos_validos_devuelve_201_y_el_contrato()
    {
        var cliente = await _factory.CrearClienteAutenticadoAsync();

        var respuesta = await cliente.PostAsync("/api/contratos", Formulario());

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        Assert.NotNull(respuesta.Headers.Location);

        using var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        var raiz = json.RootElement;

        Assert.Equal("Proveedor Nuevo", raiz.GetProperty("nombreProveedor").GetString());
        Assert.Equal(15000.50m, raiz.GetProperty("montoContrato").GetDecimal());
        Assert.Equal("contrato.pdf", raiz.GetProperty("archivoNombre").GetString());
        Assert.Equal("Activo", raiz.GetProperty("estado").GetString());
    }

    [Fact]
    public async Task La_respuesta_no_expone_la_ruta_fisica_del_archivo()
    {
        // La clave de almacenamiento es un detalle interno; publicarla facilitaria
        // sondear el almacenamiento.
        var cliente = await _factory.CrearClienteAutenticadoAsync();

        var respuesta = await cliente.PostAsync("/api/contratos", Formulario());
        var texto = await respuesta.Content.ReadAsStringAsync();

        Assert.DoesNotContain("archivoRuta", texto, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("contratos/20", texto, StringComparison.Ordinal);
    }

    [Fact]
    public async Task El_documento_subido_se_puede_descargar_intacto()
    {
        var cliente = await _factory.CrearClienteAutenticadoAsync();
        var id = await CrearContratoAsync(cliente);

        var descarga = await cliente.GetAsync($"/api/contratos/{id}/archivo");

        Assert.Equal(HttpStatusCode.OK, descarga.StatusCode);
        Assert.Equal("application/pdf", descarga.Content.Headers.ContentType?.MediaType);
        Assert.Equal(ContenidoPdf, await descarga.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task Con_download_true_se_envia_el_nombre_original()
    {
        var cliente = await _factory.CrearClienteAutenticadoAsync();

        var creado = await cliente.PostAsync("/api/contratos",
            Formulario(nombreArchivo: "Contrato Alpha.pdf"));
        using var json = JsonDocument.Parse(await creado.Content.ReadAsStringAsync());
        var id = json.RootElement.GetProperty("id").GetGuid();

        var descarga = await cliente.GetAsync($"/api/contratos/{id}/archivo?download=true");

        Assert.Equal("attachment", descarga.Content.Headers.ContentDisposition?.DispositionType);
        Assert.Contains("Contrato Alpha.pdf",
            descarga.Content.Headers.ContentDisposition?.ToString() ?? string.Empty,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task Descargar_el_archivo_de_un_contrato_inexistente_devuelve_404()
    {
        var cliente = await _factory.CrearClienteAutenticadoAsync();

        var respuesta = await cliente.GetAsync($"/api/contratos/{Guid.CreateVersion7()}/archivo");

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }

    [Fact]
    public async Task Descargar_sin_token_devuelve_401()
    {
        var cliente = await _factory.CrearClienteAutenticadoAsync();
        var id = await CrearContratoAsync(cliente);

        // Los documentos nunca son accesibles sin autenticacion.
        var anonimo = await _factory.CreateClient().GetAsync($"/api/contratos/{id}/archivo");

        Assert.Equal(HttpStatusCode.Unauthorized, anonimo.StatusCode);
    }

    [Fact]
    public async Task Crear_sin_archivo_devuelve_400()
    {
        var cliente = await _factory.CrearClienteAutenticadoAsync();

        var respuesta = await cliente.PostAsync("/api/contratos",
            Formulario(incluirArchivo: false));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    public async Task Crear_con_un_ejecutable_renombrado_devuelve_400()
    {
        var cliente = await _factory.CrearClienteAutenticadoAsync();
        byte[] ejecutable = [0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00];

        var respuesta = await cliente.PostAsync("/api/contratos",
            Formulario(contenido: ejecutable));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    public async Task Crear_con_extension_no_permitida_devuelve_400()
    {
        var cliente = await _factory.CrearClienteAutenticadoAsync();

        var respuesta = await cliente.PostAsync("/api/contratos",
            Formulario(nombreArchivo: "script.exe", contentType: "application/octet-stream"));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-100")]
    public async Task Crear_con_monto_invalido_devuelve_400(string monto)
    {
        var cliente = await _factory.CrearClienteAutenticadoAsync();

        var respuesta = await cliente.PostAsync("/api/contratos", Formulario(monto: monto));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    public async Task Crear_con_vencimiento_anterior_al_inicio_devuelve_400()
    {
        var cliente = await _factory.CrearClienteAutenticadoAsync();

        var respuesta = await cliente.PostAsync("/api/contratos", Formulario(
            fechaInicio: "2026-12-31",
            fechaVencimiento: "2026-01-01"));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Theory]
    [InlineData("15000.50", 15000.50)]
    [InlineData("0.01", 0.01)]
    [InlineData("1234567.89", 1234567.89)]
    public async Task El_monto_se_interpreta_igual_sea_cual_sea_el_locale_del_servidor(
        string enviado, double esperado)
    {
        // Regresion: el binding de modelos usa la cultura del servidor. En una
        // maquina con locale espanol, "15000.50" se leia como 1500050 porque el
        // punto se tomaba como separador de miles. La API fija cultura invariante.
        var cliente = await _factory.CrearClienteAutenticadoAsync();

        var respuesta = await cliente.PostAsync("/api/contratos", Formulario(monto: enviado));
        respuesta.EnsureSuccessStatusCode();

        using var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        var monto = json.RootElement.GetProperty("montoContrato").GetDecimal();

        Assert.Equal((decimal)esperado, monto);
    }

    [Fact]
    public async Task Un_contrato_rechazado_no_se_persiste()
    {
        var cliente = await _factory.CrearClienteAutenticadoAsync();

        var antes = await cliente.GetFromJsonAsync<Dictionary<string, int>>(
            "/api/contratos/resumen");

        await cliente.PostAsync("/api/contratos", Formulario(monto: "0"));

        var despues = await cliente.GetFromJsonAsync<Dictionary<string, int>>(
            "/api/contratos/resumen");

        Assert.Equal(antes!["total"], despues!["total"]);
    }
}
