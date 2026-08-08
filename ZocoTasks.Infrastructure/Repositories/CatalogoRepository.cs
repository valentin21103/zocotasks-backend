using Microsoft.EntityFrameworkCore;
using ZocoTasks.Business.DTOs;
using ZocoTasks.Business.Interfaces;
using ZocoTasks.Infrastructure.Data;

namespace ZocoTasks.Infrastructure.Repositories;

public class CatalogoRepository : ICatalogoRepository
{
    private readonly ZocoDbContext _context;

    public CatalogoRepository(ZocoDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<EstadoCatalogoDto>> ObtenerEstados(CancellationToken ct)
    {
        return await _context.EstadosComercio
            .AsNoTracking()
            .OrderBy(e => e.Orden)
            .Select(e => new EstadoCatalogoDto
            {
                Id = (int)e.Id,
                Codigo = e.Codigo,
                Nombre = e.Nombre,
                Orden = e.Orden,
                EsFinal = e.EsFinal
            })
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<CatalogoItemDto>> ObtenerRubros(CancellationToken ct)
    {
        // Solo los activos: un rubro dado de baja no debe ofrecerse para
        // comercios nuevos, aunque los historicos lo sigan referenciando.
        return await _context.Rubros
            .AsNoTracking()
            .Where(r => r.Activo)
            .OrderBy(r => r.Nombre)
            .Select(r => new CatalogoItemDto { Id = r.Id, Nombre = r.Nombre })
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<CatalogoItemDto>> ObtenerTiposInteraccion(CancellationToken ct)
    {
        return await _context.TiposInteraccion
            .AsNoTracking()
            .OrderBy(t => t.Id)
            .Select(t => new CatalogoItemDto { Id = (int)t.Id, Nombre = t.Nombre })
            .ToListAsync(ct);
    }
}
