using System.Diagnostics.CodeAnalysis;
using Contratos.Application.Common;
using Contratos.Application.Contratos;
using Contratos.Domain.Entities;
using Contratos.Domain.Enums;
using Contratos.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace Contratos.Infrastructure.Persistence;

public class ContratoRepository : IContratoRepository
{
    private readonly AppDbContext _context;

    public ContratoRepository(AppDbContext context) => _context = context;

    public async Task<ResultadoPaginado<Contrato>> BuscarAsync(
        ContratoFiltro filtro,
        DateOnly hoy,
        CancellationToken cancellationToken)
    {
        var consulta = AplicarFiltros(_context.Contratos.AsNoTracking(), filtro, hoy);

        // El total se cuenta en la base con los mismos filtros: nunca se traen
        // todas las filas a memoria para contarlas.
        var total = await consulta.CountAsync(cancellationToken);

        var items = await consulta
            // Orden estable: sin un desempate por Id, dos contratos con la misma
            // fecha podrian aparecer repetidos o ausentes entre paginas.
            .OrderBy(c => c.FechaVencimiento)
            .ThenBy(c => c.Id)
            .Skip((filtro.Page - 1) * filtro.PageSize)
            .Take(filtro.PageSize)
            .ToListAsync(cancellationToken);

        return new ResultadoPaginado<Contrato>(items, filtro.Page, filtro.PageSize, total);
    }

    public async Task AgregarAsync(Contrato contrato, CancellationToken cancellationToken)
    {
        _context.Contratos.Add(contrato);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<Contrato?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken) =>
        _context.Contratos
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<ContratoResumenDto> ResumirAsync(
        DateOnly hoy,
        CancellationToken cancellationToken)
    {
        var limitePorVencer = hoy.AddDays(ContratoEstadoCalculator.DiasAvisoVencimiento);

        // Una sola consulta agregada en lugar de cinco COUNT separados.
        var conteos = await _context.Contratos
            .AsNoTracking()
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Inactivos = g.Count(c => c.Inactivo),
                Vencidos = g.Count(c => !c.Inactivo && c.FechaVencimiento < hoy),
                PorVencer = g.Count(c => !c.Inactivo
                                         && c.FechaVencimiento >= hoy
                                         && c.FechaVencimiento <= limitePorVencer),
                Activos = g.Count(c => !c.Inactivo && c.FechaVencimiento > limitePorVencer)
            })
            .FirstOrDefaultAsync(cancellationToken);

        return conteos is null
            ? new ContratoResumenDto(0, 0, 0, 0, 0)
            : new ContratoResumenDto(
                conteos.Total,
                conteos.Activos,
                conteos.PorVencer,
                conteos.Vencidos,
                conteos.Inactivos);
    }

    /// <summary>
    /// Construye la consulta filtrada sin ejecutarla. Es internal para que los
    /// tests puedan inspeccionar el SQL generado con ToQueryString().
    /// </summary>
    internal IQueryable<Contrato> ConsultaFiltrada(ContratoFiltro filtro, DateOnly hoy) =>
        AplicarFiltros(_context.Contratos.AsNoTracking(), filtro, hoy);

    [SuppressMessage("Globalization", "CA1304:Especificar CultureInfo",
        Justification = "ToLower() se traduce a lower() de SQL; la sobrecarga con " +
                        "CultureInfo no es traducible por EF Core.")]
    [SuppressMessage("Globalization", "CA1311:Especificar referencia cultural",
        Justification = "Igual que CA1304: la comparacion la resuelve el motor de " +
                        "base de datos, no .NET.")]
    [SuppressMessage("Performance", "CA1862:Usar StringComparison",
        Justification = "Contains(string, StringComparison) lanza excepcion al " +
                        "traducirse a SQL en EF Core.")]
    private static IQueryable<Contrato> AplicarFiltros(
        IQueryable<Contrato> consulta,
        ContratoFiltro filtro,
        DateOnly hoy)
    {
        if (!string.IsNullOrWhiteSpace(filtro.Proveedor))
        {
            // Coincidencia parcial sin distinguir mayusculas. Se usa ToLower en
            // ambos lados en lugar de ILIKE para que la consulta sea portable
            // entre PostgreSQL y el SQLite de los tests de integracion.
            // Ambas llamadas las ejecuta el motor de base de datos, no .NET.
            var termino = filtro.Proveedor.Trim().ToLowerInvariant();
            consulta = consulta.Where(c => c.NombreProveedor.ToLower().Contains(termino));
        }

        if (filtro.FechaInicioDesde is { } inicioDesde)
        {
            consulta = consulta.Where(c => c.FechaInicio >= inicioDesde);
        }

        if (filtro.FechaInicioHasta is { } inicioHasta)
        {
            consulta = consulta.Where(c => c.FechaInicio <= inicioHasta);
        }

        if (filtro.FechaVencimientoDesde is { } vencimientoDesde)
        {
            consulta = consulta.Where(c => c.FechaVencimiento >= vencimientoDesde);
        }

        if (filtro.FechaVencimientoHasta is { } vencimientoHasta)
        {
            consulta = consulta.Where(c => c.FechaVencimiento <= vencimientoHasta);
        }

        if (filtro.Estado is { } estado)
        {
            consulta = AplicarFiltroEstado(consulta, estado, hoy);
        }

        return consulta;
    }

    /// <summary>
    /// Traduce el estado a predicados sobre columnas reales.
    ///
    /// Es la pieza clave del listado: el estado no existe como columna, asi que
    /// filtrarlo en memoria obligaria a traer toda la tabla y rompería la
    /// paginacion y el conteo total. Estas condiciones las resuelve PostgreSQL
    /// apoyandose en el indice (inactivo, fecha_vencimiento).
    /// </summary>
    private static IQueryable<Contrato> AplicarFiltroEstado(
        IQueryable<Contrato> consulta,
        ContratoEstado estado,
        DateOnly hoy)
    {
        // Se calcula fuera de la expresion para que EF lo envie como parametro
        // y no intente traducir AddDays a SQL.
        var limitePorVencer = hoy.AddDays(ContratoEstadoCalculator.DiasAvisoVencimiento);

        return estado switch
        {
            ContratoEstado.Inactivo =>
                consulta.Where(c => c.Inactivo),

            ContratoEstado.Vencido =>
                consulta.Where(c => !c.Inactivo && c.FechaVencimiento < hoy),

            ContratoEstado.PorVencer =>
                consulta.Where(c => !c.Inactivo
                                    && c.FechaVencimiento >= hoy
                                    && c.FechaVencimiento <= limitePorVencer),

            ContratoEstado.Activo =>
                consulta.Where(c => !c.Inactivo && c.FechaVencimiento > limitePorVencer),

            _ => consulta
        };
    }
}
