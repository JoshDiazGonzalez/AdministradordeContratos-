using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Contratos.Application.Auth;
using Contratos.Domain.Entities;
using Contratos.Domain.Services;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Contratos.Infrastructure.Auth;

public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly JwtOptions _options;
    private readonly IDateTimeProvider _reloj;

    public JwtTokenGenerator(IOptions<JwtOptions> options, IDateTimeProvider reloj)
    {
        _options = options.Value;
        _reloj = reloj;
    }

    public (string Token, DateTimeOffset ExpiraEn) Generar(Usuario usuario)
    {
        var expiraEn = _reloj.AhoraUtc.AddMinutes(_options.ExpirationMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, usuario.Username),
            // Identificador unico del token, util para trazabilidad.
            new(JwtRegisteredClaimNames.Jti, Guid.CreateVersion7().ToString()),
            new(ClaimTypes.Name, usuario.Username)
        };

        var credenciales = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: _reloj.AhoraUtc.UtcDateTime,
            expires: expiraEn.UtcDateTime,
            signingCredentials: credenciales);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiraEn);
    }
}
