using Contratos.Domain.Entities;

namespace Contratos.Application.Auth;

/// <summary>Genera los tokens de acceso de la aplicacion.</summary>
public interface IJwtTokenGenerator
{
    (string Token, DateTimeOffset ExpiraEn) Generar(Usuario usuario);
}
