using Microsoft.EntityFrameworkCore;
using ZocoTasks.Business.Interfaces;
using ZocoTasks.Domain.Entities;
using ZocoTasks.Infrastructure.Data;

namespace ZocoTasks.Infrastructure.Repositories;

public class UsuarioRepository : IUsuarioRepository
{
    private readonly ZocoDbContext _context;

    public UsuarioRepository(ZocoDbContext context)
    {
        _context = context;
    }

    public async Task<Usuario?> BuscarPorEmail(string email, CancellationToken ct)
    {
        // La columna es citext, asi que la comparacion ignora mayusculas sola.
        return await _context.Usuarios
            .AsNoTracking()
            .Include(u => u.UsuarioRoles)
                .ThenInclude(ur => ur.Rol)
            .FirstOrDefaultAsync(u => u.Email == email, ct);
    }
}
