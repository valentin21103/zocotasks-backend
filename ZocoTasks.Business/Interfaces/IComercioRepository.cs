using ZocoTasks.Business.DTOs;
using ZocoTasks.Domain.Entities;

namespace ZocoTasks.Business.Interfaces;

/// <summary>
/// Acceso a datos de comercios.
/// </summary>
/// <remarks>
/// Es un repositorio concreto y no un <c>IGenericRepository&lt;T&gt;</c> a
/// proposito: la consulta principal del sistema necesita includes, full text
/// search, orden dinamico y paginacion, y nada de eso entra en la firma de un
/// generico. Un generico habria obligado a exponer <c>IQueryable</c>, y ahi la
/// abstraccion deja de abstraer.
/// </remarks>
public interface IComercioRepository
{
    /// <summary>Listado con busqueda, filtros, orden y paginacion resueltos en la base.</summary>
    Task<PagedResult<ComercioListItemDto>> Listar(ComercioFiltroDto filtro, CancellationToken ct);

    /// <summary>Trae el comercio con su rubro, su estado y sus interacciones.</summary>
    Task<Comercio?> ObtenerConDetalle(int id, CancellationToken ct);

    /// <summary>Sin includes: para editar o cambiar de estado no hace falta traer el resto.</summary>
    Task<Comercio?> ObtenerParaEditar(int id, CancellationToken ct);

    Task<bool> ExisteCuit(string cuit, int? exceptoId, CancellationToken ct);

    Task<bool> ExisteRubro(int rubroId, CancellationToken ct);

    void Agregar(Comercio comercio);

    /// <summary>
    /// Persiste los cambios usando <paramref name="versionEsperada"/> como valor
    /// original del token de concurrencia. Si otro usuario modifico la fila en
    /// el medio, EF lanza <c>DbUpdateConcurrencyException</c>.
    /// </summary>
    Task GuardarConControlDeConcurrencia(Comercio comercio, uint versionEsperada, CancellationToken ct);

    Task Guardar(CancellationToken ct);
}
