using Contratos.Application.Storage;
using Contratos.Domain.Services;
using Contratos.Infrastructure.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Contratos.Tests.Storage;

public class LocalFileStorageServiceTests : IDisposable
{
    private sealed class RelojFijo : IDateTimeProvider
    {
        public DateOnly Hoy => new(2026, 6, 15);

        public DateTimeOffset AhoraUtc => new(2026, 6, 15, 10, 0, 0, TimeSpan.Zero);
    }

    private readonly string _carpeta = Path.Combine(
        Path.GetTempPath(), $"contratos-tests-{Guid.CreateVersion7():N}");

    private LocalFileStorageService Crear() => new(
        Options.Create(new StorageOptions { LocalPath = _carpeta }),
        new RelojFijo(),
        NullLogger<LocalFileStorageService>.Instance);

    private static ArchivoSubido Pdf(string nombre = "contrato.pdf") =>
        new(nombre, "application/pdf", 9, new MemoryStream("%PDF-1.7\n"u8.ToArray()));

    public void Dispose()
    {
        if (Directory.Exists(_carpeta))
        {
            Directory.Delete(_carpeta, recursive: true);
        }

        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task Guardar_devuelve_una_ruta_y_escribe_el_archivo()
    {
        var almacen = Crear();

        var ruta = await almacen.GuardarAsync(Pdf(), CancellationToken.None);

        Assert.StartsWith("contratos/2026/06/", ruta, StringComparison.Ordinal);
        Assert.EndsWith(".pdf", ruta, StringComparison.Ordinal);

        await using var leido = await almacen.AbrirAsync(ruta, CancellationToken.None);
        Assert.NotNull(leido);
    }

    [Fact]
    public async Task La_ruta_generada_no_contiene_el_nombre_original()
    {
        // Regla de seguridad: el nombre que envia el cliente nunca forma parte
        // de la ruta fisica.
        var almacen = Crear();

        var ruta = await almacen.GuardarAsync(Pdf("Contrato Confidencial 2026.pdf"),
            CancellationToken.None);

        Assert.DoesNotContain("Confidencial", ruta, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(" ", ruta, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("../../../appsettings.json")]
    [InlineData(@"..\..\..\web.config")]
    [InlineData("../secretos.pdf")]
    public async Task Un_nombre_con_path_traversal_no_escapa_de_la_carpeta(string nombre)
    {
        var almacen = Crear();

        var ruta = await almacen.GuardarAsync(Pdf(nombre), CancellationToken.None);

        Assert.StartsWith("contratos/2026/06/", ruta, StringComparison.Ordinal);
        Assert.DoesNotContain("..", ruta, StringComparison.Ordinal);

        // El archivo quedo dentro de la carpeta configurada.
        var escritos = Directory.GetFiles(_carpeta, "*", SearchOption.AllDirectories);
        Assert.All(escritos, f => Assert.StartsWith(
            Path.GetFullPath(_carpeta), Path.GetFullPath(f), StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("../../appsettings.json")]
    [InlineData(@"..\..\web.config")]
    [InlineData("/etc/passwd")]
    public async Task Abrir_una_ruta_manipulada_se_bloquea(string rutaMaliciosa)
    {
        // Simula una fila alterada en base de datos con una ruta fuera de la raiz.
        var almacen = Crear();

        await Assert.ThrowsAnyAsync<Exception>(
            () => almacen.AbrirAsync(rutaMaliciosa, CancellationToken.None));
    }

    [Fact]
    public async Task Abrir_una_ruta_inexistente_devuelve_null()
    {
        var almacen = Crear();

        var resultado = await almacen.AbrirAsync(
            "contratos/2026/06/inexistente.pdf", CancellationToken.None);

        Assert.Null(resultado);
    }

    [Fact]
    public async Task Eliminar_borra_el_archivo_y_es_idempotente()
    {
        var almacen = Crear();
        var ruta = await almacen.GuardarAsync(Pdf(), CancellationToken.None);

        await almacen.EliminarAsync(ruta, CancellationToken.None);
        Assert.Null(await almacen.AbrirAsync(ruta, CancellationToken.None));

        // Borrar dos veces no debe fallar.
        await almacen.EliminarAsync(ruta, CancellationToken.None);
    }

    [Fact]
    public async Task Dos_archivos_con_el_mismo_nombre_no_colisionan()
    {
        var almacen = Crear();

        var primera = await almacen.GuardarAsync(Pdf(), CancellationToken.None);
        var segunda = await almacen.GuardarAsync(Pdf(), CancellationToken.None);

        Assert.NotEqual(primera, segunda);
    }
}
