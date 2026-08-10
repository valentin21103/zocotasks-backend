using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZocoTasks.Business.DTOs;
using ZocoTasks.Business.Interfaces;

namespace ZocoTasks.API.Controllers;

/// <summary>
/// Listas para los combos del frontend.
/// </summary>
/// <remarks>
/// Existen para que el front no hardcodee los estados ni los rubros. Si se
/// agrega un rubro en la base, aparece en el formulario sin tocar el frontend
/// ni hacer un deploy.
/// </remarks>
[Authorize]
[ApiController]
[Route("api/catalogos")]
[Produces("application/json")]
public class CatalogosController : ControllerBase
{
    private readonly ICatalogoService _service;

    public CatalogosController(ICatalogoService service)
    {
        _service = service;
    }

    [HttpGet("estados")]
    [ProducesResponseType(typeof(IReadOnlyList<EstadoCatalogoDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Estados(CancellationToken ct)
    {
        return Ok(await _service.ObtenerEstados(ct));
    }

    [HttpGet("rubros")]
    [ProducesResponseType(typeof(IReadOnlyList<CatalogoItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Rubros(CancellationToken ct)
    {
        return Ok(await _service.ObtenerRubros(ct));
    }

    [HttpGet("tipos-interaccion")]
    [ProducesResponseType(typeof(IReadOnlyList<CatalogoItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> TiposInteraccion(CancellationToken ct)
    {
        return Ok(await _service.ObtenerTiposInteraccion(ct));
    }
}
