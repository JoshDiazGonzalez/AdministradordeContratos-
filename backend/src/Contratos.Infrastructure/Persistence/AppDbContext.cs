using Microsoft.EntityFrameworkCore;

namespace Contratos.Infrastructure.Persistence;

/// <summary>
/// Contexto de EF Core de la aplicacion.
/// Las entidades y sus configuraciones se agregan en la Fase 4.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Aplica todas las clases IEntityTypeConfiguration<T> de este ensamblado
        // (Persistence/Configurations) sin tener que registrarlas una por una.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
