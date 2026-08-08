using ZocoTasks.Business.DTOs;

namespace ZocoTasks.Business.Interfaces;

public interface IInteraccionService
{
    Task<IReadOnlyList<InteraccionDto>> ListarPorComercio(int comercioId, CancellationToken ct);

    Task<InteraccionDto> Crear(int comercioId, CrearInteraccionDto dto, CancellationToken ct);

    Task Eliminar(int comercioId, int interaccionId, CancellationToken ct);
}
