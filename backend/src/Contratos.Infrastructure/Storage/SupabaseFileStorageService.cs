using System.Net;
using System.Net.Http.Headers;
using Contratos.Application.Storage;
using Contratos.Domain.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Contratos.Infrastructure.Storage;

/// <summary>
/// Almacenamiento en Supabase Storage mediante su API REST.
///
/// Se usa HttpClient directamente en lugar del SDK: la operacion es subir,
/// descargar y borrar un objeto, y una dependencia adicional no aporta valor.
///
/// El bucket es privado. Los archivos se sirven a traves de la API .NET, que ya
/// exige JWT; un bucket publico dejaria los contratos accesibles con solo
/// conocer la URL.
/// </summary>
public class SupabaseFileStorageService : IFileStorageService
{
    public const string HttpClientName = "SupabaseStorage";

    private readonly HttpClient _http;
    private readonly SupabaseOptions _opciones;
    private readonly IDateTimeProvider _reloj;
    private readonly ILogger<SupabaseFileStorageService> _logger;

    public SupabaseFileStorageService(
        HttpClient http,
        IOptions<SupabaseOptions> opciones,
        IDateTimeProvider reloj,
        ILogger<SupabaseFileStorageService> logger)
    {
        _http = http;
        _opciones = opciones.Value;
        _reloj = reloj;
        _logger = logger;
    }

    public async Task<string> GuardarAsync(
        ArchivoSubido archivo,
        CancellationToken cancellationToken)
    {
        var ruta = GeneradorDeRutas.Generar(archivo.NombreOriginal, _reloj.AhoraUtc);

        using var contenido = new StreamContent(archivo.Contenido);
        contenido.Headers.ContentType = new MediaTypeHeaderValue(archivo.ContentType);

        var respuesta = await _http.PostAsync(
            $"object/{_opciones.StorageBucket}/{ruta}", contenido, cancellationToken);

        if (!respuesta.IsSuccessStatusCode)
        {
            var motivo = await respuesta.Content.ReadAsStringAsync(cancellationToken);
            LogStorage.FalloSubidaSupabase(_logger, $"{(int)respuesta.StatusCode} {motivo}");

            throw new InvalidOperationException(
                "No se pudo guardar el documento en el almacenamiento.");
        }

        LogStorage.ArchivoGuardado(_logger, ruta, archivo.TamanoBytes);
        return ruta;
    }

    public async Task<Stream?> AbrirAsync(string ruta, CancellationToken cancellationToken)
    {
        var respuesta = await _http.GetAsync(
            $"object/{_opciones.StorageBucket}/{ruta}",
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        if (respuesta.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        respuesta.EnsureSuccessStatusCode();
        return await respuesta.Content.ReadAsStreamAsync(cancellationToken);
    }

    public async Task EliminarAsync(string ruta, CancellationToken cancellationToken)
    {
        var respuesta = await _http.DeleteAsync(
            $"object/{_opciones.StorageBucket}/{ruta}", cancellationToken);

        // Que ya no exista no es un error: el resultado buscado es el mismo.
        if (respuesta.StatusCode != HttpStatusCode.NotFound)
        {
            respuesta.EnsureSuccessStatusCode();
        }
    }
}
