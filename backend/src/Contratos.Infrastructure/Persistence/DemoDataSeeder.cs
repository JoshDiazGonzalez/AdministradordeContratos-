using Contratos.Application.Storage;
using Contratos.Domain.Entities;
using Contratos.Domain.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Contratos.Infrastructure.Persistence;

/// <summary>
/// Carga contratos ficticios que cubren los cuatro estados de vigencia.
///
/// Reglas de seguridad:
///  - Solo actua si Seed:DatosDemo es true (desactivado por defecto).
///  - Solo actua si la tabla de contratos esta vacia: nunca mezcla datos de
///    demostracion con datos reales.
///
/// Las fechas se calculan respecto al dia actual. Con fechas fijas, un contrato
/// "por vencer" pasaria a "vencido" con el tiempo y la demostracion dejaria de
/// mostrar los cuatro estados.
/// </summary>
public class DemoDataSeeder
{
    private readonly AppDbContext _context;
    private readonly IFileStorageService _almacenamiento;
    private readonly IDateTimeProvider _reloj;
    private readonly SeedOptions _opciones;
    private readonly ILogger<DemoDataSeeder> _logger;

    public DemoDataSeeder(
        AppDbContext context,
        IFileStorageService almacenamiento,
        IDateTimeProvider reloj,
        IOptions<SeedOptions> opciones,
        ILogger<DemoDataSeeder> logger)
    {
        _context = context;
        _almacenamiento = almacenamiento;
        _reloj = reloj;
        _opciones = opciones.Value;
        _logger = logger;
    }

    private sealed record ContratoDemo(
        string Proveedor,
        decimal Monto,
        int DiasDesdeInicio,
        int DiasHastaVencimiento,
        string Descripcion,
        bool Inactivo = false);

    private static readonly ContratoDemo[] Contratos =
    [
        // Activos: vencen mas alla de la ventana de aviso de 30 dias.
        new("Proveedor Alpha", 12_500.00m, -180, 185,
            "Licenciamiento anual de software de gestión documental."),
        new("Servicios Tecnológicos Omega", 48_900.00m, -30, 700,
            "Soporte y mantenimiento de la plataforma de banca en línea."),
        new("Seguridad Integral del Pacífico", 22_340.75m, -90, 275,
            "Servicio de vigilancia y control de accesos en agencias."),

        // Por vencer: dentro de los proximos 30 dias.
        new("Proveedor Beta", 8_340.50m, -340, 12,
            "Mantenimiento preventivo de cajeros automáticos."),
        new("Limpieza y Mantenimiento Andino", 3_150.00m, -355, 28,
            "Limpieza de oficinas administrativas."),

        // Vencidos: la fecha de vencimiento ya paso.
        new("Proveedor Gamma", 150_000.00m, -400, -35,
            "Implementación del sistema de reportes regulatorios."),
        new("Consultora Financiera del Sur", 18_000.00m, -200, -5,
            "Consultoría para la actualización de políticas de crédito."),

        // Inactivo: desactivado por negocio aunque su vigencia no haya terminado.
        new("Proveedor Delta", 2_200.00m, -60, 300,
            "Suministro de papelería. Contrato suspendido por cambio de proveedor.",
            Inactivo: true),
    ];

    public async Task SembrarAsync(CancellationToken cancellationToken = default)
    {
        if (!_opciones.DatosDemo)
        {
            return;
        }

        if (await _context.Contratos.AnyAsync(cancellationToken))
        {
            LogDemo.Omitido(_logger);
            return;
        }

        var hoy = _reloj.Hoy;
        var ahora = _reloj.AhoraUtc;
        var rutasGuardadas = new List<string>();

        try
        {
            foreach (var demo in Contratos)
            {
                var inicio = hoy.AddDays(demo.DiasDesdeInicio);
                var vencimiento = hoy.AddDays(demo.DiasHastaVencimiento);

                var pdf = DocumentoDemo.Generar(
                    $"Contrato - {demo.Proveedor}",
                    $"Vigencia: {inicio:yyyy-MM-dd} a {vencimiento:yyyy-MM-dd}");

                var nombreArchivo = $"contrato-{Slug(demo.Proveedor)}.pdf";

                await using var contenido = new MemoryStream(pdf);
                var ruta = await _almacenamiento.GuardarAsync(
                    new ArchivoSubido(nombreArchivo, "application/pdf", pdf.Length, contenido),
                    cancellationToken);
                rutasGuardadas.Add(ruta);

                var contrato = Contrato.Crear(
                    demo.Proveedor,
                    demo.Monto,
                    inicio,
                    vencimiento,
                    demo.Descripcion,
                    nombreArchivo,
                    ruta,
                    "application/pdf",
                    pdf.Length,
                    ahora);

                if (demo.Inactivo)
                {
                    contrato.CambiarInactivo(inactivo: true, ahora);
                }

                _context.Contratos.Add(contrato);
            }

            await _context.SaveChangesAsync(cancellationToken);
            LogDemo.Creados(_logger, Contratos.Length);
        }
        catch
        {
            // Si falla a mitad, no quedan documentos huerfanos en el almacenamiento.
            foreach (var ruta in rutasGuardadas)
            {
                await _almacenamiento.EliminarAsync(ruta, CancellationToken.None);
            }

            throw;
        }
    }

    private static string Slug(string texto) =>
        string.Concat(texto
            .Normalize(System.Text.NormalizationForm.FormD)
            .Where(c => char.IsAsciiLetterOrDigit(c) || c == ' ')
            .Select(c => c == ' ' ? '-' : char.ToLowerInvariant(c)));
}

internal static partial class LogDemo
{
    [LoggerMessage(
        EventId = 3100,
        Level = LogLevel.Information,
        Message = "Datos de demostracion omitidos: ya existen contratos.")]
    public static partial void Omitido(ILogger logger);

    [LoggerMessage(
        EventId = 3101,
        Level = LogLevel.Information,
        Message = "Se crearon {Cantidad} contratos de demostracion.")]
    public static partial void Creados(ILogger logger, int cantidad);
}
