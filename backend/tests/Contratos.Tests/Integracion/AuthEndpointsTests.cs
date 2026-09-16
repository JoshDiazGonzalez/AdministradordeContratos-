using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Contratos.Application.Auth;

namespace Contratos.Tests.Integracion;

/// <summary>
/// Recorrido HTTP real contra la API levantada en memoria.
/// </summary>
public class AuthEndpointsTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public AuthEndpointsTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Login_con_credenciales_correctas_devuelve_200_y_token()
    {
        var cliente = _factory.CreateClient();

        var respuesta = await cliente.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(ApiFactory.AdminUsername, ApiFactory.AdminPassword));

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);

        var cuerpo = await respuesta.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(cuerpo);
        Assert.False(string.IsNullOrWhiteSpace(cuerpo!.Token));
        Assert.Equal("admin", cuerpo.User.Username);
        Assert.True(cuerpo.ExpiresAt > DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Login_con_password_incorrecta_devuelve_401_ProblemDetails()
    {
        var cliente = _factory.CreateClient();

        var respuesta = await cliente.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(ApiFactory.AdminUsername, "incorrecta"));

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        Assert.Equal("application/problem+json",
            respuesta.Content.Headers.ContentType?.MediaType);

        var texto = await respuesta.Content.ReadAsStringAsync();
        // Nunca debe filtrar el hash ni la contrasena enviada.
        Assert.DoesNotContain("$2", texto, StringComparison.Ordinal);
        Assert.DoesNotContain("incorrecta", texto, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Login_con_usuario_inexistente_devuelve_401_no_404()
    {
        var cliente = _factory.CreateClient();

        var respuesta = await cliente.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("fantasma", ApiFactory.AdminPassword));

        // 404 revelaria que el usuario no existe.
        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    public async Task Login_con_campos_vacios_devuelve_400_con_errores_por_campo()
    {
        var cliente = _factory.CreateClient();

        var respuesta = await cliente.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("", ""));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);

        var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        var errores = json.RootElement.GetProperty("errors");
        Assert.True(errores.TryGetProperty("Username", out _));
        Assert.True(errores.TryGetProperty("Password", out _));
    }

    [Fact]
    public async Task Endpoint_protegido_sin_token_devuelve_401()
    {
        var cliente = _factory.CreateClient();

        var respuesta = await cliente.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    public async Task Endpoint_protegido_con_token_valido_devuelve_200()
    {
        var cliente = _factory.CreateClient();

        var login = await cliente.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(ApiFactory.AdminUsername, ApiFactory.AdminPassword));
        var token = (await login.Content.ReadFromJsonAsync<LoginResponse>())!.Token;

        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var respuesta = await cliente.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Contains("admin", await respuesta.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Endpoint_protegido_con_token_manipulado_devuelve_401()
    {
        var cliente = _factory.CreateClient();

        var login = await cliente.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(ApiFactory.AdminUsername, ApiFactory.AdminPassword));
        var token = (await login.Content.ReadFromJsonAsync<LoginResponse>())!.Token;

        // Se altera un caracter de la firma: la validacion debe rechazarlo.
        var manipulado = token[..^2] + (token[^2] == 'a' ? 'b' : 'a') + token[^1];

        cliente.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", manipulado);
        var respuesta = await cliente.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    public async Task El_endpoint_de_salud_responde_sin_autenticacion()
    {
        var cliente = _factory.CreateClient();

        var respuesta = await cliente.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }
}
