using FluentValidation;

namespace Contratos.Application.Auth;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("El usuario es obligatorio.")
            .MaximumLength(50).WithMessage("El usuario no puede superar los 50 caracteres.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("La contraseña es obligatoria.")
            .MaximumLength(128).WithMessage("La contraseña no puede superar los 128 caracteres.");
    }
}
