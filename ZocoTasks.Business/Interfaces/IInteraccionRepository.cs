using ZocoTasks.Business.DTOs;
using ZocoTasks.Domain.Entities;

namespace ZocoTasks.Business.Interfaces;

public interface IInteraccionRepository
{
    Task<IReadOnlyList<InteraccionDto>> ListarPorComercio(int comercioId, CancellationToken ct);

    Task<Interaccion?> ObtenerPorId(int comercioId, int interaccionId, CancellationToken ct);

    /// <summary>Verifica que el comercio exista y no este dado de baja.</summary>
    Task<bool> ExisteComercio(int comercioId, CancellationToken ct);

    void Agregar(Interaccion interaccion);

    void Eliminar(Interaccion interaccion);

    Task Guardar(CancellationToken ct);
}
