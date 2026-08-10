using Microsoft.EntityFrameworkCore;
using ZocoTasks.Business.DTOs;
using ZocoTasks.Business.Interfaces;
using ZocoTasks.Domain.Entities;
using ZocoTasks.Infrastructure.Data;

namespace ZocoTasks.Infrastructure.Repositories;

public class RubroRepository : IRubroRepository
{
    private readonly ZocoDbContext _context;

    public RubroRepository(ZocoDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Trae los activos y los dados de baja: es la pantalla de administracion,
    /// donde ocultar los inactivos dejaria al usuario sin forma de reactivarlos.
    /// </summary>
    public async Task<IReadOnlyList<RubroAbmDto>> Listar(CancellationToken ct)
    {
        return await _context.Rubros
            .AsNoTracking()
            .OrderBy(r => r.Nombre)
            .Select(r => new RubroAbmDto
            {
                Id = r.Id,
                Nombre = r.Nombre,
                Activo = r.Activo,
                // El filtro global de baja logica de Comercio se aplica solo,
                // asi que los comercios eliminados no entran en la cuenta.
                CantidadComercios = r.Comercios.Count()
            })
            .ToListAsync(ct);
    }

    public async Task<Rubro?> ObtenerPorId(int id, CancellationToken ct)
    {
        return await _context.Rubros.FirstOrDefaultAsync(r => r.Id == id, ct);
    }

    public async Task<bool> ExisteNombre(string nombre, int? exceptoId, CancellationToken ct)
    {
        var normalizado = nombre.ToLower();

        return await _context.Rubros
            .AnyAsync(r => r.Nombre.ToLower() == normalizado
                        && (exceptoId == null || r.Id != exceptoId), ct);
    }

    public async Task<int> ContarComercios(int rubroId, CancellationToken ct)
    {
        return await _context.Comercios.CountAsync(c => c.RubroId == rubroId, ct);
    }

    public void Agregar(Rubro rubro) => _context.Rubros.Add(rubro);

    public void Eliminar(Rubro rubro) => _context.Rubros.Remove(rubro);

    public async Task Guardar(CancellationToken ct) => await _context.SaveChangesAsync(ct);
}
