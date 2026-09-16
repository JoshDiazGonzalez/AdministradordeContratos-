using Contratos.Application.Common;

namespace Contratos.Application.Storage;

/// <summary>
/// Valida los documentos subidos. Se ejecuta SIEMPRE en el backend: las
/// restricciones del navegador (accept, maxSize) son una comodidad para el
/// usuario, nunca un control de seguridad.
/// </summary>
public class ValidadorDeArchivo
{
    private readonly StorageOptions _opciones;

    public ValidadorDeArchivo(StorageOptions opciones) => _opciones = opciones;

    /// <summary>
    /// Firmas binarias de los formatos aceptados. Se comprueban porque tanto la
    /// extension como el Content-Type los controla el cliente: renombrar
    /// virus.exe a contrato.pdf y declarar application/pdf es trivial.
    /// </summary>
    private static readonly Dictionary<string, byte[][]> FirmasPorExtension = new()
    {
        // "%PDF-"
        [".pdf"] = [[0x25, 0x50, 0x44, 0x46, 0x2D]],

        // Documento OLE2 (Word 97-2003)
        [".doc"] = [[0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1]],

        // ZIP: los .docx son paquetes OOXML comprimidos
        [".docx"] = [[0x50, 0x4B, 0x03, 0x04], [0x50, 0x4B, 0x05, 0x06], [0x50, 0x4B, 0x07, 0x08]]
    };

    private static readonly Dictionary<string, string[]> ContentTypesPorExtension = new()
    {
        [".pdf"] = ["application/pdf"],
        [".doc"] = ["application/msword"],
        [".docx"] = ["application/vnd.openxmlformats-officedocument.wordprocessingml.document"]
    };

    public async Task ValidarAsync(ArchivoSubido? archivo, CancellationToken cancellationToken)
    {
        var errores = new Dictionary<string, string[]>();

        if (archivo is null || archivo.TamanoBytes == 0)
        {
            errores["archivo"] = ["Debe seleccionar un documento."];
            throw new ValidacionException(errores);
        }

        if (archivo.TamanoBytes > _opciones.MaxFileSizeBytes)
        {
            var maximoMb = _opciones.MaxFileSizeBytes / 1024d / 1024d;
            errores["archivo"] = [$"El documento no puede superar {maximoMb:0.#} MB."];
        }

        var extension = Path.GetExtension(archivo.NombreOriginal).ToLowerInvariant();

        if (!_opciones.AllowedExtensions.Contains(extension))
        {
            var permitidas = string.Join(", ", _opciones.AllowedExtensions);
            errores["archivo"] = [$"Solo se aceptan documentos {permitidas}."];
            throw new ValidacionException(errores);
        }

        if (ContentTypesPorExtension.TryGetValue(extension, out var esperados)
            && !esperados.Contains(archivo.ContentType, StringComparer.OrdinalIgnoreCase))
        {
            errores["archivo"] =
                [$"El tipo de contenido no corresponde a un archivo {extension}."];
        }

        if (!await FirmaCoincideAsync(archivo, extension, cancellationToken))
        {
            errores["archivo"] =
                [$"El contenido del archivo no corresponde a un documento {extension} valido."];
        }

        if (errores.Count > 0)
        {
            throw new ValidacionException(errores);
        }
    }

    private static async Task<bool> FirmaCoincideAsync(
        ArchivoSubido archivo,
        string extension,
        CancellationToken cancellationToken)
    {
        if (!FirmasPorExtension.TryGetValue(extension, out var firmas))
        {
            return true;
        }

        var longitudMaxima = firmas.Max(f => f.Length);
        var buffer = new byte[longitudMaxima];

        var leidos = await archivo.Contenido.ReadAtLeastAsync(
            buffer, longitudMaxima, throwOnEndOfStream: false, cancellationToken);

        // El stream debe volver al inicio: despues se guarda su contenido completo.
        if (archivo.Contenido.CanSeek)
        {
            archivo.Contenido.Position = 0;
        }

        return leidos >= longitudMaxima
               && firmas.Any(firma => buffer.Take(firma.Length).SequenceEqual(firma));
    }
}
