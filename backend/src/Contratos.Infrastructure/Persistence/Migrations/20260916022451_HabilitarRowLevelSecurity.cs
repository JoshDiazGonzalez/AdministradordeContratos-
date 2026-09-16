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
            migrationBuilder.Sql("REVOKE ALL ON public.contratos FROM anon, authenticated;");
            migrationBuilder.Sql("REVOKE ALL ON public.usuarios FROM anon, authenticated;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE public.contratos DISABLE ROW LEVEL SECURITY;");
            migrationBuilder.Sql("ALTER TABLE public.usuarios DISABLE ROW LEVEL SECURITY;");
        }
    }
}
