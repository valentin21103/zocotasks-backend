using Microsoft.AspNetCore.Mvc;
using ZocoTasks.Business.DTOs;
using ZocoTasks.Business.Interfaces;

namespace ZocoTasks.API.Controllers;

/// <summary>
/// ABM de rubros.
/// </summary>
/// <remarks>
/// Separado de <c>CatalogosController</c> a proposito: aquel es solo lectura y
/// alimenta los combos; este modifica datos y, cuando se implemente la
/// autenticacion, va restringido a Admin.
///
/// TODO al implementar JWT: [Authorize(Roles = "Admin")] a nivel de clase.
/// Hoy no se puede poner porque no hay ningun esquema de autenticacion
/// registrado y el atributo fallaria en tiempo de ejecucion. El frontend ya
/// esconde la pantalla para los moderadores, pero eso es usabilidad: la
/// autorizacion real tiene que estar aca.
/// </remarks>
[ApiController]
[Route("api/rubros")]
[Produces("application/json")]
public class RubrosController : ControllerBase
{
    private readonly IRubroService _service;

    public RubrosController(IRubroService service)
    {
        _service = service;
    }

    /// <summary>Todos los rubros, activos e inactivos, con su uso.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<RubroAbmDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(CancellationToken ct)
    {
        return Ok(await _service.Listar(ct));
    }

    [HttpPost]
    [ProducesResponseType(typeof(RubroAbmDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Crear([FromBody] GuardarRubroDto dto, CancellationToken ct)
    {
        var rubro = await _service.Crear(dto, ct);

        return CreatedAtAction(nameof(Listar), new { id = rubro.Id }, rubro);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(RubroAbmDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Actualizar(
        int id, [FromBody] GuardarRubroDto dto, CancellationToken ct)
    {
        return Ok(await _service.Actualizar(id, dto, ct));
    }

    /// <summary>
    /// Borra el rubro si no lo usa nadie; si tiene comercios asociados lo
    /// desactiva. La respuesta dice cual de las dos cosas paso.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(ResultadoBajaRubroDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Eliminar(int id, CancellationToken ct)
    {
        return Ok(await _service.Eliminar(id, ct));
    }
}
