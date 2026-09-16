using Contratos.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Contratos.Infrastructure.Persistence;

/// <summary>
/// Contexto de EF Core de la aplicacion.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Contrato> Contratos => Set<Contrato>();

    public DbSet<Usuario> Usuarios => Set<Usuario>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Aplica todas las clases IEntityTypeConfiguration<T> de este ensamblado
        // (Persistence/Configurations) sin tener que registrarlas una por una.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
