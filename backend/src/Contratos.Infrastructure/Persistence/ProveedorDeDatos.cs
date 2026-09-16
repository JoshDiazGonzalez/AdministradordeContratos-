namespace Contratos.Infrastructure.Persistence;

/// <summary>
/// Indica con que motor quedo configurada la aplicacion.
///
/// El arranque lo consulta para decidir como preparar el esquema: con PostgreSQL
/// se aplican las migraciones versionadas; con la base local de desarrollo se
/// crea desde el modelo de EF, porque esas migraciones contienen SQL especifico
/// de PostgreSQL.
/// </summary>
/// <param name="UsaPostgreSql">true si hay una cadena de conexion configurada.</param>
/// <param name="RutaBaseLocal">
/// Ruta del archivo de la base local, o null si se usa PostgreSQL.
/// </param>
public record ProveedorDeDatos(bool UsaPostgreSql, string? RutaBaseLocal);
