namespace Contratos.Application.Storage;

/// <summary>
/// Almacenamiento de documentos. El dominio no conoce si detras hay disco local
/// o Supabase Storage: solo maneja la clave devuelta por Guardar.
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Guarda el archivo y devuelve la clave generada.
    /// La clave NUNCA se construye con el nombre original del archivo.
    /// </summary>
    Task<string> GuardarAsync(ArchivoSubido archivo, CancellationToken cancellationToken);

    Task<Stream?> AbrirAsync(string ruta, CancellationToken cancellationToken);

    Task EliminarAsync(string ruta, CancellationToken cancellationToken);
}
