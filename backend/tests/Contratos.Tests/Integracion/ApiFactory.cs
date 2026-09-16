using System.Net.Http.Json;
using System.Text.Json;
using Contratos.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Contratos.Tests.Integracion;

/// <summary>
/// Levanta la API real en memoria y sustituye PostgreSQL por SQLite.
///
/// El objetivo no es probar SQL especifico de Postgres (eso ya se verifico contra
/// Supabase), sino el recorrido HTTP completo: enrutado, validacion, middleware de
/// errores, emision y verificacion del JWT, y el seed del usuario inicial.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string AdminUsername = "admin";
    public const string AdminPassword = "Admin123*";

    // La conexion se mantiene abierta: una base SQLite en memoria vive mientras
    // exista al menos una conexion.
    private readonly SqliteConnection _conexion = new("DataSource=:memory:");

    /// <summary>
    /// Program.cs registra la infraestructura mientras se construye el host, antes
    /// de que se apliquen los ConfigureAppConfiguration de la factory. Como si lee
    /// variables de entorno, la configuracion de prueba se define aqui.
    /// </summary>
    static ApiFactory()
    {
        var valores = new Dictionary<string, string>
        {
            // Valor ficticio: AddInfrastructure exige que exista, pero el
            // DbContext se reemplaza por SQLite mas abajo.
            ["ConnectionStrings__DefaultConnection"] =
                "Host=localhost;Database=noop;Username=noop;Password=noop",
            ["Jwt__Secret"] = "clave-de-pruebas-con-mas-de-32-caracteres!!",
            ["Jwt__Issuer"] = "ContratosApi",
            ["Jwt__Audience"] = "ContratosApp",
            ["Jwt__ExpirationMinutes"] = "60",
            ["Seed__AdminUsername"] = AdminUsername,
            ["Seed__AdminPassword"] = AdminPassword,
            ["Seed__AdminFullName"] = "Administrador"
        };

        foreach (var (clave, valor) in valores)
        {
            Environment.SetEnvironmentVariable(clave, valor);
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // AddDbContext registra varios descriptores ademas de
            // DbContextOptions<T> (entre ellos la configuracion de opciones y los
            // servicios del proveedor). Si queda alguno, EF detecta dos proveedores
            // registrados y falla. Se eliminan todos los relacionados.
            var aQuitar = services
                .Where(d =>
                    d.ServiceType == typeof(AppDbContext) ||
                    (d.ServiceType.FullName?.Contains("DbContextOptions", StringComparison.Ordinal) ?? false))
                .ToList();

            foreach (var descriptor in aQuitar)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<AppDbContext>(options => options
                .UseSqlite(_conexion)
                .UseSnakeCaseNamingConvention());

            // El esquema se crea desde el modelo de EF; las migraciones son SQL
            // de Postgres y no aplican aqui.
            using var proveedor = services.BuildServiceProvider();
            using var scope = proveedor.CreateScope();
            var contexto = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            contexto.Database.EnsureCreated();
        });
    }

    public async Task InitializeAsync()
    {
        await _conexion.OpenAsync();

        // Acceder a Services construye el host y crea el esquema.
        using var scope = Services.CreateScope();
        var contexto = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await SembrarDatosAsync(contexto);
    }

    /// <summary>Punto de extension para que una factory derivada cargue datos.</summary>
    protected virtual Task SembrarDatosAsync(AppDbContext contexto) => Task.CompletedTask;

    /// <summary>Ejecuta una accion sobre el contexto de la base de pruebas.</summary>
    public async Task ConContextoAsync(Func<AppDbContext, Task> accion)
    {
        using var scope = Services.CreateScope();
        var contexto = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await accion(contexto);
    }

    /// <summary>Devuelve un cliente ya autenticado como administrador.</summary>
    public async Task<HttpClient> CrearClienteAutenticadoAsync()
    {
        var cliente = CreateClient();

        var respuesta = await cliente.PostAsJsonAsync("/api/auth/login",
            new { username = AdminUsername, password = AdminPassword });
        respuesta.EnsureSuccessStatusCode();

        using var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        var token = json.RootElement.GetProperty("token").GetString();

        cliente.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        return cliente;
    }

    async Task IAsyncLifetime.DisposeAsync() => await _conexion.DisposeAsync();
}
