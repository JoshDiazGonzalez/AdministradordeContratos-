using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Contratos.Application.Storage;
using Contratos.Domain.Enums;
using Contratos.Domain.Services;
using Contratos.Infrastructure.Persistence;
using Contratos.Infrastructure.Storage;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Contratos.Tests.Application;

public sealed class DemoDataSeederTests : IDisposable
{
    private sealed class RelojFijo : IDateTimeProvider
    {
        public DateOnly Hoy => new(2026, 6, 15);

        public DateTimeOffset AhoraUtc => new(2026, 6, 15, 12, 0, 0, TimeSpan.Zero);
    }

    private readonly SqliteConnection _conexion = new("DataSource=:memory:");
    private readonly string _carpeta = Path.Combine(
        Path.GetTempPath(), $"demo-seed-{Guid.CreateVersion7():N}");
    private readonly AppDbContext _contexto;
    private readonly LocalFileStorageService _almacen;

    public DemoDataSeederTests()
    {
        _conexion.Open();
        _contexto = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_conexion)
            .UseSnakeCaseNamingConvention()
            .Options);
        _contexto.Database.EnsureCreated();

        _almacen = new LocalFileStorageService(
            Options.Create(new StorageOptions { LocalPath = _carpeta }),
            new RelojFijo(),
            NullLogger<LocalFileStorageService>.Instance);
    }

    public void Dispose()
    {
        _contexto.Dispose();
        _conexion.Dispose();
        if (Directory.Exists(_carpeta))
        {
            Directory.Delete(_carpeta, recursive: true);
        }
    }

    private DemoDataSeeder Crear(bool datosDemo) => new(
        _contexto,
        _almacen,
        new RelojFijo(),
        Options.Create(new SeedOptions { DatosDemo = datosDemo }),
        NullLogger<DemoDataSeeder>.Instance);

    [Fact]
    public async Task Desactivado_por_defecto_no_crea_nada()
    {
        // Proteccion: nunca debe cargar datos ficticios si nadie lo pidio.
        Assert.False(new SeedOptions().DatosDemo);

        await Crear(datosDemo: false).SembrarAsync();

        Assert.Equal(0, await _contexto.Contratos.CountAsync());
    }

    [Fact]
    public async Task Activado_crea_contratos_que_cubren_los_cuatro_estados()
    {
        await Crear(datosDemo: true).SembrarAsync();

        var hoy = new RelojFijo().Hoy;
        var estados = (await _contexto.Contratos.ToListAsync())
            .Select(c => c.EstadoEn(hoy))
            .ToHashSet();

        Assert.Equal(
            [ContratoEstado.Activo, ContratoEstado.PorVencer, ContratoEstado.Vencido, ContratoEstado.Inactivo],
            estados.OrderBy(e => e));
    }

    [Fact]
    public async Task No_se_ejecuta_si_ya_existen_contratos()
    {
        // Nunca mezcla datos de demostracion con datos reales.
        var seeder = Crear(datosDemo: true);
        await seeder.SembrarAsync();
        var total = await _contexto.Contratos.CountAsync();

        await seeder.SembrarAsync();

        Assert.Equal(total, await _contexto.Contratos.CountAsync());
    }

    [Fact]
    public async Task Cada_contrato_tiene_su_documento_guardado()
    {
        await Crear(datosDemo: true).SembrarAsync();

        foreach (var contrato in await _contexto.Contratos.ToListAsync())
        {
            await using var documento = await _almacen.AbrirAsync(contrato.ArchivoRuta, CancellationToken.None);
            Assert.NotNull(documento);
            Assert.Equal(contrato.ArchivoTamanoBytes, documento!.Length);
        }
    }

    [Fact]
    public async Task El_documento_generado_supera_la_validacion_de_archivos_de_la_API()
    {
        // Si los PDF de demostracion no pasaran el validador, serian distintos de
        // lo que un usuario real puede subir.
        var pdf = DocumentoDemo.Generar("Contrato - Proveedor Alpha", "Vigencia de prueba");
        var validador = new ValidadorDeArchivo(new StorageOptions());

        await validador.ValidarAsync(
            new ArchivoSubido("contrato.pdf", "application/pdf", pdf.Length, new MemoryStream(pdf)),
            CancellationToken.None);
    }

    [Fact]
    public void La_tabla_xref_apunta_exactamente_a_cada_objeto_del_PDF()
    {
        var pdf = DocumentoDemo.Generar("Contrato (Beta) \\ con ñ y tildes", "Detalle");
        var texto = Encoding.ASCII.GetString(pdf);

        var inicioXref = int.Parse(
            Regex.Match(texto, @"startxref\n(\d+)").Groups[1].Value, CultureInfo.InvariantCulture);
        Assert.StartsWith("xref", texto[inicioXref..], StringComparison.Ordinal);

        var posiciones = Regex.Matches(texto, @"(\d{10}) 00000 n")
            .Select(m => int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture))
            .ToList();

        Assert.NotEmpty(posiciones);
        for (var i = 0; i < posiciones.Count; i++)
        {
            Assert.StartsWith($"{i + 1} 0 obj", texto[posiciones[i]..], StringComparison.Ordinal);
        }
    }

    [Fact]
    public void El_texto_con_parentesis_y_barras_se_escapa_y_no_rompe_el_PDF()
    {
        var pdf = Encoding.ASCII.GetString(DocumentoDemo.Generar("A (b) c\\d", "x"));

        Assert.Contains(@"(A \(b\) c\\d) Tj", pdf, StringComparison.Ordinal);
    }
}
