using Contratos.Application.Auth;
using Contratos.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Contratos.Infrastructure.Persistence;

public class UsuarioRepository : IUsuarioRepository
{
    private readonly AppDbContext _context;

    public UsuarioRepository(AppDbContext context) => _context = context;

    public Task<Usuario?> ObtenerPorUsernameAsync(
        string username,
        CancellationToken cancellationToken) =>
        _context.Usuarios
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == username, cancellationToken);
}
