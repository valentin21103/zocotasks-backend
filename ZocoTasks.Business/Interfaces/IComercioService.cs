using ZocoTasks.Business.DTOs;
using ZocoTasks.Domain.Enums;

namespace ZocoTasks.Business.Interfaces;

public interface IComercioService
{
    Task<PagedResult<ComercioListItemDto>> Listar(ComercioFiltroDto filtro, CancellationToken ct);

    Task<ComercioDetalleDto> ObtenerPorId(int id, CancellationToken ct);

    Task<ComercioDetalleDto> Crear(CrearComercioDto dto, CancellationToken ct);

    /// <param name="versionEsperada">
    /// Valor que el cliente recibio en el ETag. Si no coincide con el actual,
    /// se lanza <c>DbUpdateConcurrencyException</c> y la API responde 409.
    /// </param>
    Task<ComercioDetalleDto> Actualizar(
        int id, ActualizarComercioDto dto, uint versionEsperada, CancellationToken ct);

    Task<ComercioDetalleDto> CambiarEstado(
        int id, EstadoComercioEnum nuevoEstado, uint versionEsperada, CancellationToken ct);

    /// <summary>Baja logica: marca fecha_eliminacion, no borra la fila.</summary>
    Task Eliminar(int id, CancellationToken ct);
}
