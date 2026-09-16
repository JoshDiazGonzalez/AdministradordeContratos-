namespace Contratos.Application.Storage;

/// <summary>
/// Archivo recibido del cliente, desacoplado de IFormFile para que la capa de
/// aplicacion no dependa de ASP.NET Core.
/// </summary>
public record ArchivoSubido(
    string NombreOriginal,
    string ContentType,
    long TamanoBytes,
    Stream Contenido);

/// <summary>Archivo recuperado del almacenamiento.</summary>
public record ArchivoDescargado(
    Stream Contenido,
    string ContentType,
    string NombreArchivo);
