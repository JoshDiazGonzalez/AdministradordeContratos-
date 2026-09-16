using FluentValidation;

namespace Contratos.Application.Contratos;

public class CrearContratoRequestValidator : AbstractValidator<CrearContratoRequest>
{
    public CrearContratoRequestValidator()
    {
        RuleFor(x => x.NombreProveedor)
            .NotEmpty().WithMessage("El nombre del proveedor es obligatorio.")
            .MaximumLength(200)
            .WithMessage("El nombre del proveedor no puede superar los 200 caracteres.");

        RuleFor(x => x.MontoContrato)
            .GreaterThan(0).WithMessage("El monto debe ser mayor que 0.")
            .LessThanOrEqualTo(9_999_999_999_999_999.99m)
            .WithMessage("El monto excede el máximo admitido.");

        RuleFor(x => x.FechaInicio)
            .NotEqual(default(DateOnly)).WithMessage("La fecha de inicio es obligatoria.");

        RuleFor(x => x.FechaVencimiento)
            .NotEqual(default(DateOnly)).WithMessage("La fecha de vencimiento es obligatoria.")
            .GreaterThanOrEqualTo(x => x.FechaInicio)
            .WithMessage("La fecha de vencimiento no puede ser anterior a la fecha de inicio.");

        RuleFor(x => x.Descripcion)
            .NotEmpty().WithMessage("La descripción es obligatoria.")
            .MaximumLength(1000)
            .WithMessage("La descripción no puede superar los 1000 caracteres.");
    }
}
