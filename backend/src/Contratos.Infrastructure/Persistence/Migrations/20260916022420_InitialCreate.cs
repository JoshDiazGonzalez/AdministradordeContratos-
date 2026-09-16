using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Contratos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "contratos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre_proveedor = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    monto_contrato = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    fecha_inicio = table.Column<DateOnly>(type: "date", nullable: false),
                    fecha_vencimiento = table.Column<DateOnly>(type: "date", nullable: false),
                    inactivo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    descripcion = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    archivo_nombre = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    archivo_ruta = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    archivo_content_type = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    archivo_tamano_bytes = table.Column<long>(type: "bigint", nullable: false),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    fecha_actualizacion = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_contratos", x => x.id);
                    table.CheckConstraint("ck_contratos_archivo_tamano", "archivo_tamano_bytes > 0");
                    table.CheckConstraint("ck_contratos_monto_positivo", "monto_contrato > 0");
                    table.CheckConstraint("ck_contratos_rango_fechas", "fecha_vencimiento >= fecha_inicio");
                });

            migrationBuilder.CreateTable(
                name: "usuarios",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    username = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    nombre_completo = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_usuarios", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_contratos_fecha_inicio",
                table: "contratos",
                column: "fecha_inicio");

            migrationBuilder.CreateIndex(
                name: "ix_contratos_inactivo_fecha_vencimiento",
                table: "contratos",
                columns: new[] { "inactivo", "fecha_vencimiento" });

            migrationBuilder.CreateIndex(
                name: "ix_contratos_nombre_proveedor",
                table: "contratos",
                column: "nombre_proveedor");

            migrationBuilder.CreateIndex(
                name: "ix_usuarios_username",
                table: "usuarios",
                column: "username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "contratos");

            migrationBuilder.DropTable(
                name: "usuarios");
        }
    }
}
