using Microsoft.AspNetCore.Mvc;
using ZocoTasks.Business.DTOs;
using ZocoTasks.Business.Interfaces;

namespace ZocoTasks.API.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _service;

    public AuthController(IAuthService service)
    {
        _service = service;
    }

    /// <summary>
    /// Devuelve el token JWT. Es el unico endpoint publico junto con el health.
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginRespuestaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginDto dto, CancellationToken ct)
    {
        var respuesta = await _service.Login(dto, ct);

        if (respuesta is null)
        {
            // Mismo mensaje si el email no existe o si la contrasenia esta mal:
            // distinguirlos permitiria averiguar que emails estan registrados.
            return Unauthorized(new { mensaje = "Email o contrasenia incorrectos." });
        }

        return Ok(respuesta);
    }
}
