using Contratos.Domain.Services;
using Contratos.Infrastructure.Persistence;
using Contratos.Infrastructure.Services;
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
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Falta la cadena de conexion. Defina ConnectionStrings__DefaultConnection " +
                "como variable de entorno (vea .env.example). Nunca la escriba en appsettings.json.");
        }

        var options = new DatabaseOptions();
        configuration.GetSection(DatabaseOptions.SectionName).Bind(options);

        services.AddDbContext<AppDbContext>(builder =>
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

            // Postgres distingue mayusculas si el identificador va entre comillas.
            // EF genera PascalCase por defecto, lo que obligaria a citar cada columna
            // en toda consulta SQL manual. snake_case evita ese problema.
            builder.UseSnakeCaseNamingConvention();

            if (options.EnableSensitiveDataLogging)
            {
                builder.EnableSensitiveDataLogging();
            }
        });

        // Reloj del negocio (America/Guayaquil). Singleton: no tiene estado mutable.
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

        return services;
    }
}
