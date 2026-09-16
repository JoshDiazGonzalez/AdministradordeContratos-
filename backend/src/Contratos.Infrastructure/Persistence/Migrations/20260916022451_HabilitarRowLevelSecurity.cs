using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Contratos.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Habilita Row Level Security en las tablas de la aplicacion, sin crear politicas.
    ///
    /// Motivo: estas tablas viven en el esquema "public", que Supabase puede exponer
    /// a traves de la Data API. La seguridad real del sistema es el JWT y [Authorize]
    /// de la API .NET, porque Angular nunca consulta Supabase directamente. Aun asi,
    /// sin RLS una exposicion accidental de la Data API dejaria los contratos y los
    /// hashes de contrasena accesibles con la clave publica.
    ///
    /// RLS activado y sin politicas equivale a denegar todo para los roles anon y
    /// authenticated. La API no se ve afectada: se conecta con un rol propietario,
    /// que omite RLS por definicion.
    /// </summary>
    public partial class HabilitarRowLevelSecurity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE public.contratos ENABLE ROW LEVEL SECURITY;");
            migrationBuilder.Sql("ALTER TABLE public.usuarios ENABLE ROW LEVEL SECURITY;");

            // Defensa adicional: retirar cualquier permiso heredado por los roles
            // publicos, de modo que la tabla no sea alcanzable ni aunque se
            // habilite la Data API.
            //
            // Los roles anon y authenticated solo existen en Supabase. Contra un
            // PostgreSQL normal (desarrollo local, Docker, CI) no estan, y un
            // REVOKE directo abortaria la migracion. Se comprueba antes de actuar
            // para que el mismo historial sirva en ambos entornos.
            migrationBuilder.Sql("""
                DO $$
                DECLARE
                    rol text;
                    tabla text;
                BEGIN
                    FOREACH rol IN ARRAY ARRAY['anon', 'authenticated'] LOOP
                        IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = rol) THEN
                            FOREACH tabla IN ARRAY ARRAY['contratos', 'usuarios'] LOOP
                                EXECUTE format(
                                    'REVOKE ALL ON public.%I FROM %I', tabla, rol);
                            END LOOP;
                        END IF;
                    END LOOP;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE public.contratos DISABLE ROW LEVEL SECURITY;");
            migrationBuilder.Sql("ALTER TABLE public.usuarios DISABLE ROW LEVEL SECURITY;");
        }
    }
}
