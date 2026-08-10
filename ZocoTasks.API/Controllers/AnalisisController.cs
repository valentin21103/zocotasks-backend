using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZocoTasks.Business.DTOs;
using ZocoTasks.Business.Interfaces;

namespace ZocoTasks.API.Controllers;

/// <summary>
/// "Analizar oportunidad".
/// </summary>
/// <remarks>
/// Es POST y no GET aunque no cree ningun recurso: la operacion no es idempotente
/// en costo (cada llamada consume cuota del proveedor) y no debe cachearse en
/// intermediarios. Un GET invitaria al navegador y a los proxies a repetirla.
///
/// Solo se ejecuta cuando el usuario aprieta el boton. No hay analisis
/// automatico al abrir la ficha.
/// </remarks>
[Authorize]
[ApiController]
[Route("api/comercios/{comercioId:int}/analizar")]
[Produces("application/json")]
public class AnalisisController : ControllerBase
{
    private readonly IAnalisisService _service;

    public AnalisisController(IAnalisisService service)
    {
        _service = service;
    }

    /// <summary>
    /// Analiza la oportunidad con la informacion actual del comercio.
    /// </summary>
    /// <remarks>
    /// Si el proveedor de IA falla devuelve 200 con <c>esDegradado: true</c> y
    /// el nivel en <c>Indeterminado</c>, no un error: que un servicio externo
    /// se caiga no es una falla de esta aplicacion.
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(AnalisisOportunidadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Analizar(int comercioId, CancellationToken ct)
    {
        return Ok(await _service.Analizar(comercioId, ct));
    }
}
