using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZocoTasks.Business.DTOs;
using ZocoTasks.Business.Interfaces;

namespace ZocoTasks.API.Controllers;

/// <summary>
/// Interacciones de un comercio.
/// </summary>
/// <remarks>
/// La ruta cuelga de <c>/api/comercios/{comercioId}</c> porque una interaccion
/// no existe por si sola: siempre pertenece a un comercio. La URL refleja el
/// 1:N del modelo en lugar de esconderlo.
/// </remarks>
[Authorize]
[ApiController]
[Route("api/comercios/{comercioId:int}/interacciones")]
[Produces("application/json")]
public class InteraccionesController : ControllerBase
{
    private readonly IInteraccionService _service;

    public InteraccionesController(IInteraccionService service)
    {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<InteraccionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Listar(int comercioId, CancellationToken ct)
    {
        var interacciones = await _service.ListarPorComercio(comercioId, ct);
        return Ok(interacciones);
    }

    [HttpPost]
    [ProducesResponseType(typeof(InteraccionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Crear(
        int comercioId, [FromBody] CrearInteraccionDto dto, CancellationToken ct)
    {
        var creada = await _service.Crear(comercioId, dto, ct);

        return CreatedAtAction(
            nameof(Listar), new { comercioId }, creada);
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{interaccionId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Eliminar(
        int comercioId, int interaccionId, CancellationToken ct)
    {
        await _service.Eliminar(comercioId, interaccionId, ct);
        return NoContent();
    }
}
