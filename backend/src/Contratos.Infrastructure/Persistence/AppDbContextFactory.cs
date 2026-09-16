using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Contratos.Infrastructure.Persistence;

/// <summary>
/// Fabrica usada exclusivamente por las herramientas de linea de comandos de EF Core
/// (dotnet ef migrations add / script). No participa en la ejecucion de la aplicacion.
///
/// Permite generar migraciones sin tener credenciales reales: si no hay cadena de
/// conexion en el entorno, usa un destino ficticio. Generar el SQL no requiere
/// conectarse; aplicarlo si.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    private const string DesignTimeFallback =
        "Host=localhost;Port=5432;Database=contratos_design_time;Username=postgres;Password=postgres";

    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? DesignTimeFallback;

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", "public"))
            .UseSnakeCaseNamingConvention()
            .Options;

        return new AppDbContext(options);
    }
}
