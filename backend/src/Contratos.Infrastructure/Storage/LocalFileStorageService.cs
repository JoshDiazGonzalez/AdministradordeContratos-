using Contratos.Application.Storage;
using Contratos.Domain.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Contratos.Infrastructure.Storage;

/// <summary>
/// Almacenamiento en disco. Alternativa a Supabase Storage cuando el entorno no
/// tiene credenciales (desarrollo local, evaluacion offline, tests).
/// </summary>
public class LocalFileStorageService : IFileStorageService
{
    private readonly string _raiz;
    private readonly IDateTimeProvider _reloj;
    private readonly ILogger<LocalFileStorageService> _logger;

    public LocalFileStorageService(
        IOptions<StorageOptions> opciones,
        IDateTimeProvider reloj,
        ILogger<LocalFileStorageService> logger)
    {
        _reloj = reloj;
        _logger = logger;

        var configurada = opciones.Value.LocalPath;
        _raiz = Path.GetFullPath(Path.IsPathRooted(configurada)
            ? configurada
            : Path.Combine(AppContext.BaseDirectory, configurada));
    }

    public async Task<string> GuardarAsync(
        ArchivoSubido archivo,
        CancellationToken cancellationToken)
    {
        var ruta = GeneradorDeRutas.Generar(archivo.NombreOriginal, _reloj.AhoraUtc);
        var destino = ResolverRutaSegura(ruta);

        Directory.CreateDirectory(Path.GetDirectoryName(destino)!);

        await using var salida = new FileStream(
            destino, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await archivo.Contenido.CopyToAsync(salida, cancellationToken);

        LogStorage.ArchivoGuardado(_logger, ruta, archivo.TamanoBytes);
        return ruta;
    }

    public Task<Stream?> AbrirAsync(string ruta, CancellationToken cancellationToken)
    {
        var origen = ResolverRutaSegura(ruta);

        if (!File.Exists(origen))
        {
            return Task.FromResult<Stream?>(null);
        }

        Stream stream = new FileStream(
            origen, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult<Stream?>(stream);
    }

    public Task EliminarAsync(string ruta, CancellationToken cancellationToken)
    {
        var destino = ResolverRutaSegura(ruta);

        if (File.Exists(destino))
        {
            File.Delete(destino);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Convierte una clave logica en ruta absoluta y verifica que quede dentro de
    /// la carpeta raiz.
    ///
    /// Segunda barrera contra path traversal: aunque la clave se genera en el
    /// servidor, una fila manipulada en base de datos podria contener
    /// "../../secretos.txt". Comparar la ruta ya normalizada contra la raiz
    /// detiene ese caso.
    /// </summary>
    private string ResolverRutaSegura(string ruta)
    {
        if (string.IsNullOrWhiteSpace(ruta))
        {
            throw new ArgumentException("La ruta del archivo no puede estar vacia.", nameof(ruta));
        }

        var completa = Path.GetFullPath(Path.Combine(_raiz, ruta));

        var raizNormalizada = _raiz.EndsWith(Path.DirectorySeparatorChar)
            ? _raiz
            : _raiz + Path.DirectorySeparatorChar;

        if (!completa.StartsWith(raizNormalizada, StringComparison.Ordinal))
        {
            LogStorage.RutaFueraDeRaiz(_logger, ruta);
            throw new UnauthorizedAccessException(
                "La ruta del archivo queda fuera del almacenamiento permitido.");
        }

        return completa;
    }
}

internal static partial class LogStorage
{
    [LoggerMessage(
        EventId = 5000,
        Level = LogLevel.Information,
        Message = "Documento guardado en {Ruta} ({TamanoBytes} bytes)")]
    public static partial void ArchivoGuardado(ILogger logger, string ruta, long tamanoBytes);

    [LoggerMessage(
        EventId = 5001,
        Level = LogLevel.Error,
        Message = "Se bloqueo un acceso a una ruta fuera del almacenamiento: {Ruta}")]
    public static partial void RutaFueraDeRaiz(ILogger logger, string ruta);

    [LoggerMessage(
        EventId = 5002,
        Level = LogLevel.Error,
        Message = "Fallo al subir el documento a Supabase Storage: {Motivo}")]
    public static partial void FalloSubidaSupabase(ILogger logger, string motivo);
}
