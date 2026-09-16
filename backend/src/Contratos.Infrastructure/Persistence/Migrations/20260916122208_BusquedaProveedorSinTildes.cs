using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Contratos.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Anade el nombre del proveedor normalizado (minusculas, sin tildes) para
    /// que la busqueda encuentre "Pacífico" al escribir "pacifico".
    /// </summary>
    public partial class BusquedaProveedorSinTildes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_contratos_nombre_proveedor",
                table: "contratos");

            migrationBuilder.AddColumn<string>(
                name: "nombre_proveedor_busqueda",
                table: "contratos",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            // Relleno de los contratos existentes. Sin esto quedarian con la columna
            // vacia y ninguna busqueda por proveedor los encontraria.
            //
            // Replica en SQL la normalizacion de TextoBusqueda.Normalizar para los
            // caracteres del espanol y de lenguas latinas habituales. Los contratos
            // nuevos o editados se normalizan siempre desde C#.
            migrationBuilder.Sql("""
                UPDATE contratos
                SET nombre_proveedor_busqueda = trim(regexp_replace(
                    lower(translate(
                        nombre_proveedor,
                        'ÁÀÄÂÃáàäâãÉÈËÊéèëêÍÌÏÎíìïîÓÒÖÔÕóòöôõÚÙÜÛúùüûÑñÇç',
                        'AAAAAaaaaaEEEEeeeeIIIIiiiiOOOOOoooooUUUUuuuuNnCc')),
                    '\s+', ' ', 'g'));
                """);

            migrationBuilder.CreateIndex(
                name: "ix_contratos_nombre_proveedor_busqueda",
                table: "contratos",
                column: "nombre_proveedor_busqueda");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_contratos_nombre_proveedor_busqueda",
                table: "contratos");

            migrationBuilder.DropColumn(
                name: "nombre_proveedor_busqueda",
                table: "contratos");

            migrationBuilder.CreateIndex(
                name: "ix_contratos_nombre_proveedor",
                table: "contratos",
                column: "nombre_proveedor");
        }
    }
}
