using ZocoTasks.Business.DTOs;

namespace ZocoTasks.Business.Interfaces;

/// <summary>
/// ABM de rubros.
/// </summary>
/// <remarks>
/// El rubro es el unico catalogo con alta y baja: a diferencia del estado y del
/// tipo de interaccion, que son enums del dominio y cambian solo cuando cambia
/// el codigo, los rubros cambian sin que cambie el codigo. Agregar "Farmacia"
/// no deberia requerir un deploy.
/// </remarks>
public interface IRubroService
{
    Task<IReadOnlyList<RubroAbmDto>> Listar(CancellationToken ct);

    Task<RubroAbmDto> Crear(GuardarRubroDto dto, CancellationToken ct);

    Task<RubroAbmDto> Actualizar(int id, GuardarRubroDto dto, CancellationToken ct);

    Task<ResultadoBajaRubroDto> Eliminar(int id, CancellationToken ct);
}
