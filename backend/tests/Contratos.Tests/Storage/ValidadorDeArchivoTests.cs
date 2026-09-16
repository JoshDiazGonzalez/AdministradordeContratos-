using System.Text;
using Contratos.Application.Common;
using Contratos.Application.Storage;

namespace Contratos.Tests.Storage;

public class ValidadorDeArchivoTests
{
    private static readonly StorageOptions Opciones = new()
    {
        MaxFileSizeBytes = 10 * 1024 * 1024,
        AllowedExtensions = [".pdf", ".doc", ".docx"]
    };

    private static readonly ValidadorDeArchivo Validador = new(Opciones);

    /// <summary>Cabeceras reales de cada formato aceptado.</summary>
    private static readonly byte[] CabeceraPdf = "%PDF-1.7\n"u8.ToArray();
    private static readonly byte[] CabeceraDocx = [0x50, 0x4B, 0x03, 0x04, 0x14, 0x00];
    private static readonly byte[] CabeceraDoc = [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1];

    private static ArchivoSubido Archivo(
        string nombre, string contentType, byte[] contenido, long? tamano = null) =>
        new(nombre, contentType, tamano ?? contenido.Length, new MemoryStream(contenido));

    private static async Task<Dictionary<string, string[]>> ErroresDe(ArchivoSubido? archivo)
    {
        var ex = await Assert.ThrowsAsync<ValidacionException>(
            () => Validador.ValidarAsync(archivo, CancellationToken.None));

        return new Dictionary<string, string[]>(ex.Errores);
    }

    [Fact]
    public async Task Un_pdf_valido_pasa()
    {
        await Validador.ValidarAsync(
            Archivo("contrato.pdf", "application/pdf", CabeceraPdf), CancellationToken.None);
    }

    [Fact]
    public async Task Un_docx_valido_pasa()
    {
        await Validador.ValidarAsync(
            Archivo("contrato.docx",
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                CabeceraDocx),
            CancellationToken.None);
    }

    [Fact]
    public async Task Un_doc_valido_pasa()
    {
        await Validador.ValidarAsync(
            Archivo("contrato.doc", "application/msword", CabeceraDoc), CancellationToken.None);
    }

    [Fact]
    public async Task Archivo_ausente_es_invalido()
    {
        var errores = await ErroresDe(null);

        Assert.Contains("archivo", errores.Keys);
    }

    [Fact]
    public async Task Archivo_vacio_es_invalido()
    {
        var errores = await ErroresDe(Archivo("contrato.pdf", "application/pdf", []));

        Assert.Contains("archivo", errores.Keys);
    }

    [Theory]
    [InlineData("script.exe", "application/octet-stream")]
    [InlineData("imagen.png", "image/png")]
    [InlineData("hoja.xlsx", "application/vnd.ms-excel")]
    [InlineData("sin-extension", "application/pdf")]
    public async Task Extension_no_permitida_es_invalida(string nombre, string contentType)
    {
        var errores = await ErroresDe(Archivo(nombre, contentType, CabeceraPdf));

        Assert.Contains("archivo", errores.Keys);
    }

    [Fact]
    public async Task Un_ejecutable_renombrado_a_pdf_es_rechazado()
    {
        // Caso central: extension y Content-Type los controla el cliente.
        // Solo la firma binaria delata el contenido real.
        var ejecutable = new byte[] { 0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00 }; // "MZ" de un .exe

        var errores = await ErroresDe(Archivo("contrato.pdf", "application/pdf", ejecutable));

        Assert.Contains("archivo", errores.Keys);
        Assert.Contains("contenido", string.Join(' ', errores["archivo"]),
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Content_type_que_no_corresponde_a_la_extension_es_rechazado()
    {
        var errores = await ErroresDe(Archivo("contrato.pdf", "image/png", CabeceraPdf));

        Assert.Contains("archivo", errores.Keys);
    }

    [Fact]
    public async Task Un_archivo_mayor_al_limite_es_rechazado()
    {
        // Se declara el tamano sin materializar 11 MB en memoria.
        var errores = await ErroresDe(
            Archivo("contrato.pdf", "application/pdf", CabeceraPdf, tamano: 11 * 1024 * 1024));

        Assert.Contains("archivo", errores.Keys);
        Assert.Contains("MB", string.Join(' ', errores["archivo"]), StringComparison.Ordinal);
    }

    [Fact]
    public async Task La_extension_no_distingue_mayusculas()
    {
        await Validador.ValidarAsync(
            Archivo("CONTRATO.PDF", "application/pdf", CabeceraPdf), CancellationToken.None);
    }

    [Fact]
    public async Task El_stream_queda_al_inicio_tras_validar()
    {
        // Si la validacion consumiera el stream, el archivo se guardaria truncado.
        var archivo = Archivo("contrato.pdf", "application/pdf",
            Encoding.ASCII.GetBytes("%PDF-1.7\ncontenido completo del documento"));

        await Validador.ValidarAsync(archivo, CancellationToken.None);

        Assert.Equal(0, archivo.Contenido.Position);

        using var lector = new StreamReader(archivo.Contenido);
        Assert.Contains("contenido completo", await lector.ReadToEndAsync(),
            StringComparison.Ordinal);
    }
}
