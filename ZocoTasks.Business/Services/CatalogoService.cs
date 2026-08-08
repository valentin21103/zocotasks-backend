using ZocoTasks.Business.DTOs;
using ZocoTasks.Business.Interfaces;

namespace ZocoTasks.Business.Services;

public class CatalogoService : ICatalogoService
{
    private readonly ICatalogoRepository _repository;

    public CatalogoService(ICatalogoRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<EstadoCatalogoDto>> ObtenerEstados(CancellationToken ct)
    {
        return await _repository.ObtenerEstados(ct);
    }

    public async Task<IReadOnlyList<CatalogoItemDto>> ObtenerRubros(CancellationToken ct)
    {
        return await _repository.ObtenerRubros(ct);
    }

    public async Task<IReadOnlyList<CatalogoItemDto>> ObtenerTiposInteraccion(CancellationToken ct)
    {
        return await _repository.ObtenerTiposInteraccion(ct);
    }
}
