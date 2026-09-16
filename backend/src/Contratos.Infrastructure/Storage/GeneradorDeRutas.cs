namespace Contratos.Infrastructure.Storage;

/// <summary>
/// Genera la clave de almacenamiento de un documento.
///
/// Regla de seguridad: el nombre original que envia el cliente NUNCA forma parte
/// de la ruta fisica. Un nombre como "../../appsettings.json" o
/// "..\..\web.config" permitiria escribir fuera de la carpeta prevista.
/// El nombre original se guarda aparte, solo para mostrarlo y descargarlo.
/// </summary>
public static class GeneradorDeRutas
{
    public static string Generar(string nombreOriginal, DateTimeOffset ahora)
    {
        // Path.GetExtension sobre una ruta manipulada devuelve como mucho una
        // extension, nunca segmentos de directorio; aun asi se filtra a la lista
        // blanca de caracteres para no confiar en ese comportamiento.
        var extension = Path.GetExtension(nombreOriginal).ToLowerInvariant();
        extension = new string(extension.Where(c => char.IsAsciiLetterOrDigit(c) || c == '.').ToArray());

        if (string.IsNullOrWhiteSpace(extension) || extension == ".")
        {
            extension = string.Empty;
        }

        // UUIDv7: ordenado por tiempo, de modo que los archivos de un mismo dia
        // quedan agrupados y no colisionan.
        return $"contratos/{ahora:yyyy}/{ahora:MM}/{Guid.CreateVersion7():N}{extension}";
    }
}
