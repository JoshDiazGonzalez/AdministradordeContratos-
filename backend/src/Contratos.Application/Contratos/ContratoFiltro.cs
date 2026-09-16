using Contratos.Domain.Enums;

namespace Contratos.Application.Contratos;

/// <summary>
/// Filtros de busqueda del listado. Todos son opcionales y combinables.
/// </summary>
public class ContratoFiltro
{
    public const int PageSizeMaximo = 50;
    public const int PageSizePorDefecto = 10;

    /// <summary>Coincidencia parcial, sin distinguir mayusculas.</summary>
    public string? Proveedor { get; set; }

    public ContratoEstado? Estado { get; set; }

    public DateOnly? FechaInicioDesde { get; set; }

    public DateOnly? FechaInicioHasta { get; set; }

    public DateOnly? FechaVencimientoDesde { get; set; }

    public DateOnly? FechaVencimientoHasta { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = PageSizePorDefecto;
}
