using ZocoTasks.Business.DTOs;

namespace ZocoTasks.Business.Interfaces;

public interface ICatalogoRepository
{
    Task<IReadOnlyList<EstadoCatalogoDto>> ObtenerEstados(CancellationToken ct);

    Task<IReadOnlyList<CatalogoItemDto>> ObtenerRubros(CancellationToken ct);

    Task<IReadOnlyList<CatalogoItemDto>> ObtenerTiposInteraccion(CancellationToken ct);
}
