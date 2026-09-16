using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Contratos.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Habilita RLS en la tabla de historial de migraciones de EF Core.
    ///
    /// EF crea __ef_migrations_history en el esquema "public", que Supabase expone
    /// a PostgREST. Sin RLS, cualquiera con la clave publica podria leer el
    /// historial de cambios de esquema de la aplicacion. El linter de Supabase lo
    /// reporta como error (rls_disabled_in_public).
    ///
    /// Esta tabla la gestiona EF con un rol propietario, que omite RLS, asi que
    /// activarla no afecta a las migraciones.
    /// </summary>
    public partial class ProtegerHistorialDeMigraciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE public.__ef_migrations_history ENABLE ROW LEVEL SECURITY;");
            // Los roles anon y authenticated solo existen en Supabase; se
            // comprueba antes de revocar para que la migracion tambien funcione
            // contra un PostgreSQL normal.
            migrationBuilder.Sql("""
                DO $$
                DECLARE
                    rol text;
                BEGIN
                    FOREACH rol IN ARRAY ARRAY['anon', 'authenticated'] LOOP
                        IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = rol) THEN
                            EXECUTE format(
                                'REVOKE ALL ON public.__ef_migrations_history FROM %I', rol);
                        END IF;
                    END LOOP;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE public.__ef_migrations_history DISABLE ROW LEVEL SECURITY;");
        }
    }
}
