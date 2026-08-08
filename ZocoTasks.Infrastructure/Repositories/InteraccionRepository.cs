using Microsoft.EntityFrameworkCore;
using ZocoTasks.Business.DTOs;
using ZocoTasks.Business.Interfaces;
using ZocoTasks.Domain.Entities;
using ZocoTasks.Infrastructure.Data;

namespace ZocoTasks.Infrastructure.Repositories;

public class InteraccionRepository : IInteraccionRepository
{
    private readonly ZocoDbContext _context;

    public InteraccionRepository(ZocoDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<InteraccionDto>> ListarPorComercio(
        int comercioId, CancellationToken ct)
    {
        return await _context.Interacciones
            .AsNoTracking()
            .Where(i => i.ComercioId == comercioId)
            // Mas recientes primero: es el orden en que un vendedor quiere ver
            // el seguimiento. Coincide con el indice (comercio_id, fecha).
            .OrderByDescending(i => i.Fecha)
            .ThenByDescending(i => i.Id)
            .Select(i => new InteraccionDto
            {
                Id = i.Id,
                ComercioId = i.ComercioId,
                Tipo = i.Tipo,
                TipoNombre = i.TipoNavegacion.Nombre,
                Fecha = i.Fecha,
                Detalle = i.Detalle,
                FechaCreacion = i.FechaCreacion
            })
            .ToListAsync(ct);
    }

    public async Task<Interaccion?> ObtenerPorId(
        int comercioId, int interaccionId, CancellationToken ct)
    {
        // Se filtra tambien por comercioId para que no se pueda borrar la
        // interaccion de un comercio pasando el id de otro.
        return await _context.Interacciones
            .FirstOrDefaultAsync(i => i.Id == interaccionId && i.ComercioId == comercioId, ct);
    }

    public async Task<bool> ExisteComercio(int comercioId, CancellationToken ct)
    {
        // El filtro global de baja logica ya excluye los eliminados.
        return await _context.Comercios.AnyAsync(c => c.Id == comercioId, ct);
    }

    public void Agregar(Interaccion interaccion)
    {
        _context.Interacciones.Add(interaccion);
    }

    public void Eliminar(Interaccion interaccion)
    {
        _context.Interacciones.Remove(interaccion);
    }

    public async Task Guardar(CancellationToken ct)
    {
        await _context.SaveChangesAsync(ct);
    }
}
