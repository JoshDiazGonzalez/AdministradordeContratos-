using Contratos.Application.Auth;
using Contratos.Application.Contratos;
using Contratos.Application.Storage;
using Contratos.Domain.Services;
using Contratos.Infrastructure.Auth;
using Contratos.Infrastructure.Persistence;
using Contratos.Infrastructure.Services;
using Contratos.Infrastructure.Storage;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Contratos.Infrastructure;

/// <summary>
/// Punto unico de registro de la capa de infraestructura.
/// La API llama a AddInfrastructure y no conoce los detalles del proveedor de datos.
/// </summary>
public static class DependencyInjection
{
    /// <summary>Archivo de la base local usada cuando no hay PostgreSQL configurado.</summary>
    public const string ArchivoBaseDesarrollo = "contratos-dev.db";

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        bool esDesarrollo = false)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        var usaPostgreSql = EsCadenaUtilizable(connectionString);

        if (!usaPostgreSql && !esDesarrollo)
        {
            // En produccion no hay respaldo: arrancar contra una base equivocada
            // es peor que no arrancar.
            throw new InvalidOperationException(
                """
                Falta la cadena de conexion.
                Defina ConnectionStrings__DefaultConnection como variable de entorno
                (vea el README). Nunca la escriba en appsettings.json: ese archivo
                si se versiona.
                """);
        }

        var options = new DatabaseOptions();
        configuration.GetSection(DatabaseOptions.SectionName).Bind(options);

        services.AddDbContext<AppDbContext>(builder =>
        {
            if (usaPostgreSql)
            {
                builder.UseNpgsql(connectionString, npgsql =>
                {
                    npgsql.CommandTimeout(options.CommandTimeoutSeconds);

                    // Supabase es una base remota: los cortes de red transitorios se reintentan
                    // en lugar de propagarse como error al usuario.
                    npgsql.EnableRetryOnFailure(
                        maxRetryCount: options.MaxRetryCount,
                        maxRetryDelay: TimeSpan.FromSeconds(options.MaxRetryDelaySeconds),
                        errorCodesToAdd: null);

                    // snake_case tambien para la tabla de historial de migraciones,
                    // para que sea coherente con el resto del esquema.
                    npgsql.MigrationsHistoryTable("__ef_migrations_history", "public");
                });
            }
            else
            {
                // Respaldo de desarrollo: base SQLite en un archivo local.
                // Permite clonar el repositorio y ejecutar sin configurar nada.
                builder.UseSqlite($"Data Source={RutaBaseDesarrollo()}");
            }

            // Postgres distingue mayusculas si el identificador va entre comillas.
            // EF genera PascalCase por defecto, lo que obligaria a citar cada columna
            // en toda consulta SQL manual. snake_case evita ese problema.
            builder.UseSnakeCaseNamingConvention();

            if (options.EnableSensitiveDataLogging)
            {
                builder.EnableSensitiveDataLogging();
            }
        });

        // El arranque necesita saber que proveedor se eligio: la base local crea
        // su esquema desde el modelo, porque las migraciones son SQL de PostgreSQL.
        services.AddSingleton(new ProveedorDeDatos(
            usaPostgreSql,
            usaPostgreSql ? null : RutaBaseDesarrollo()));

        // Reloj del negocio (America/Guayaquil). Singleton: no tiene estado mutable.
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

        // Autenticacion.
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(
                jwt => !string.IsNullOrWhiteSpace(jwt.Secret)
                       && jwt.Secret.Length >= JwtOptions.LongitudMinimaSecret,
                $"Jwt__Secret es obligatorio y debe tener al menos " +
                $"{JwtOptions.LongitudMinimaSecret} caracteres.")
            .Validate(
                jwt => jwt.ExpirationMinutes > 0,
                "Jwt__ExpirationMinutes debe ser mayor que 0.")
            .ValidateOnStart();

        services.Configure<SeedOptions>(configuration.GetSection(SeedOptions.SectionName));

        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<IAuthService, AuthService>();

        // Contratos.
        services.AddScoped<IContratoRepository, ContratoRepository>();
        services.AddScoped<IContratoService, ContratoService>();
        services.AddScoped<DatabaseSeeder>();
        services.AddScoped<DemoDataSeeder>();

        AgregarAlmacenamiento(services, configuration);

        // Registra todos los AbstractValidator<T> de la capa de aplicacion.
        services.AddValidatorsFromAssemblyContaining<LoginRequest>();

        return services;
    }

    /// <summary>
    /// Decide si la cadena de conexion sirve para conectarse de verdad.
    ///
    /// Se considera inservible si esta vacia o si conserva un marcador de
    /// plantilla sin sustituir (por ejemplo &lt;TU_PASSWORD&gt;). Intentar conectar
    /// con un marcador produce un error de autenticacion de PostgreSQL que no
    /// dice nada sobre la causa real.
    /// </summary>
    private static bool EsCadenaUtilizable(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return false;
        }

        // Los marcadores de plantilla van entre angulos (<password>). Ningun componente
        // valido de una cadena de conexion de Npgsql los contiene.
        return !connectionString.Contains('<') && !connectionString.Contains('>');
    }

    /// <summary>
    /// Ruta del archivo de la base local. Se guarda junto al ejecutable para que
    /// sobreviva entre ejecuciones y sea facil de borrar.
    /// </summary>
    private static string RutaBaseDesarrollo() =>
        Path.Combine(AppContext.BaseDirectory, ArchivoBaseDesarrollo);

    /// <summary>
    /// Selecciona el proveedor de almacenamiento segun Storage__Provider.
    /// El dominio no cambia: ambos implementan IFileStorageService.
    /// </summary>
    private static void AgregarAlmacenamiento(
        IServiceCollection services,
        IConfiguration configuration)
    {
        var storage = new StorageOptions();
        configuration.GetSection(StorageOptions.SectionName).Bind(storage);

        services.Configure<StorageOptions>(configuration.GetSection(StorageOptions.SectionName));
        services.Configure<SupabaseOptions>(configuration.GetSection(SupabaseOptions.SectionName));

        // El validador se construye con las opciones ya resueltas: no tiene estado.
        services.AddSingleton(new ValidadorDeArchivo(storage));

        var usaSupabase = string.Equals(storage.Provider, "Supabase", StringComparison.OrdinalIgnoreCase);

        if (!usaSupabase)
        {
            services.AddScoped<IFileStorageService, LocalFileStorageService>();
            return;
        }

        var supabase = new SupabaseOptions();
        configuration.GetSection(SupabaseOptions.SectionName).Bind(supabase);

        if (string.IsNullOrWhiteSpace(supabase.Url) || string.IsNullOrWhiteSpace(supabase.ServiceRoleKey))
        {
            throw new InvalidOperationException(
                "Storage__Provider=Supabase requiere Supabase__Url y Supabase__ServiceRoleKey. " +
                "Defina ambos como variables de entorno o use Storage__Provider=Local.");
        }

        services.AddHttpClient<IFileStorageService, SupabaseFileStorageService>(cliente =>
        {
            cliente.BaseAddress = new Uri($"{supabase.Url.TrimEnd('/')}/storage/v1/");

            // La service_role_key nunca sale del backend.
            cliente.DefaultRequestHeaders.Add("apikey", supabase.ServiceRoleKey);
            cliente.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", supabase.ServiceRoleKey);

            cliente.Timeout = TimeSpan.FromSeconds(60);
        });
    }
}
