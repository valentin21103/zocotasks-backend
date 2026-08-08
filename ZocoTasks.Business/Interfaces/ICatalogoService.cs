using ZocoTasks.Business.DTOs;

namespace ZocoTasks.Business.Interfaces;

/// <summary>
/// Catalogos para los combos del front. Existe para que el frontend no tenga
/// que hardcodear las listas de estados, rubros y tipos: si se agrega un rubro,
/// aparece solo.
/// </summary>
public interface ICatalogoService
{
    Task<IReadOnlyList<EstadoCatalogoDto>> ObtenerEstados(CancellationToken ct);

    Task<IReadOnlyList<CatalogoItemDto>> ObtenerRubros(CancellationToken ct);

    Task<IReadOnlyList<CatalogoItemDto>> ObtenerTiposInteraccion(CancellationToken ct);
}
