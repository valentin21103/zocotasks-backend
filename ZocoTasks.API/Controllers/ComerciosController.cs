using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZocoTasks.Business.DTOs;
using ZocoTasks.Business.Interfaces;

namespace ZocoTasks.API.Controllers;

[Authorize]
[ApiController]
[Route("api/comercios")]
[Produces("application/json")]
public class ComerciosController : ControllerBase
{
    private readonly IComercioService _service;

    public ComerciosController(IComercioService service)
    {
        _service = service;
    }

    /// <summary>
    /// Listado con busqueda de texto, filtros por estado y rubro, orden y
    /// paginacion. Todo se resuelve en la base, no en memoria.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ComercioListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(
        [FromQuery] ComercioFiltroDto filtro, CancellationToken ct)
    {
        var resultado = await _service.Listar(filtro, ct);
        return Ok(resultado);
    }

    /// <summary>
    /// Ficha completa del comercio con sus interacciones.
    /// Devuelve el token de concurrencia en el header <c>ETag</c>: hay que
    /// guardarlo y reenviarlo en <c>If-Match</c> al editar.
    /// </summary>
    [HttpGet("{id:int}", Name = nameof(ObtenerPorId))]
    [ProducesResponseType(typeof(ComercioDetalleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObtenerPorId(int id, CancellationToken ct)
    {
        var comercio = await _service.ObtenerPorId(id, ct);

        ETagHelper.EscribirEnRespuesta(Response, comercio.Version);

        return Ok(comercio);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ComercioDetalleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Crear(
        [FromBody] CrearComercioDto dto, CancellationToken ct)
    {
        var creado = await _service.Crear(dto, ct);

        ETagHelper.EscribirEnRespuesta(Response, creado.Version);

        // 201 con Location apuntando al recurso nuevo, como corresponde a un POST.
        return CreatedAtRoute(nameof(ObtenerPorId), new { id = creado.Id }, creado);
    }

    /// <summary>
    /// Actualiza los datos del comercio. Exige el header <c>If-Match</c> con el
    /// ETag obtenido en el GET.
    /// </summary>
    /// <remarks>
    /// Si otro usuario modifico el comercio en el medio, responde
    /// <b>409 Conflict</b> en lugar de pisar sus cambios en silencio.
    /// El estado no se cambia por aca: tiene su propio endpoint porque esta
    /// sujeto a las reglas de transicion del pipeline.
    /// </remarks>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ComercioDetalleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status428PreconditionRequired)]
    public async Task<IActionResult> Actualizar(
        int id, [FromBody] ActualizarComercioDto dto, CancellationToken ct)
    {
        var versionEsperada = ETagHelper.LeerIfMatch(Request);

        var actualizado = await _service.Actualizar(id, dto, versionEsperada, ct);

        ETagHelper.EscribirEnRespuesta(Response, actualizado.Version);

        return Ok(actualizado);
    }

    /// <summary>
    /// Mueve el comercio en el pipeline. Tambien exige <c>If-Match</c>.
    /// </summary>
    /// <remarks>
    /// Responde 409 en dos casos distintos: si la transicion no existe en el
    /// pipeline, o si otro usuario modifico el registro. El campo
    /// <c>codigo</c> del ProblemDetails permite distinguirlos.
    /// </remarks>
    [HttpPatch("{id:int}/estado")]
    [ProducesResponseType(typeof(ComercioDetalleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status428PreconditionRequired)]
    public async Task<IActionResult> CambiarEstado(
        int id, [FromBody] CambiarEstadoDto dto, CancellationToken ct)
    {
        var versionEsperada = ETagHelper.LeerIfMatch(Request);

        var actualizado = await _service.CambiarEstado(id, dto, versionEsperada, ct);

        ETagHelper.EscribirEnRespuesta(Response, actualizado.Version);

        return Ok(actualizado);
    }

    /// <summary>
    /// Baja logica. El comercio deja de aparecer en las consultas, pero sus
    /// interacciones se conservan.
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Eliminar(int id, CancellationToken ct)
    {
        await _service.Eliminar(id, ct);
        return NoContent();
    }
}
