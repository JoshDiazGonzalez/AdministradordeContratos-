using FluentValidation;

namespace Contratos.Application.Contratos;

public class ContratoFiltroValidator : AbstractValidator<ContratoFiltro>
{
    public ContratoFiltroValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0).WithMessage("La página debe ser mayor que 0.");

        RuleFor(x => x.PageSize)
            .GreaterThan(0).WithMessage("El tamaño de página debe ser mayor que 0.")
            .LessThanOrEqualTo(ContratoFiltro.PageSizeMaximo)
            .WithMessage($"El tamaño de página no puede superar {ContratoFiltro.PageSizeMaximo}.");

        RuleFor(x => x.Proveedor)
            .MaximumLength(200)
            .WithMessage("El filtro de proveedor no puede superar los 200 caracteres.");

        RuleFor(x => x)
            .Must(f => f.FechaInicioDesde is null
                       || f.FechaInicioHasta is null
                       || f.FechaInicioDesde <= f.FechaInicioHasta)
            .WithMessage("El rango de fecha de inicio está invertido.")
            .WithName(nameof(ContratoFiltro.FechaInicioDesde));

        RuleFor(x => x)
            .Must(f => f.FechaVencimientoDesde is null
                       || f.FechaVencimientoHasta is null
                       || f.FechaVencimientoDesde <= f.FechaVencimientoHasta)
            .WithMessage("El rango de fecha de vencimiento está invertido.")
            .WithName(nameof(ContratoFiltro.FechaVencimientoDesde));
    }
}
