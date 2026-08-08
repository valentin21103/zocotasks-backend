using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ZocoTasks.Infrastructure.Data;

namespace ZocoTasks.API.Controllers;

/// <summary>
/// Chequeo de salud.
/// </summary>
/// <remarks>
/// Render lo usa para saber si la instancia esta viva. Ademas sirve para
/// despertar el servicio antes de una demo: tanto Render como Neon suspenden
/// el compute tras unos minutos sin trafico, y la primera peticion despues de
/// eso tarda bastante.
/// </remarks>
[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    private readonly ZocoDbContext _context;

    public HealthController(ZocoDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult Estado()
    {
        return Ok(new { estado = "ok", fecha = DateTime.UtcNow });
    }

    /// <summary>
    /// Verifica tambien la conexion a la base. Separado del anterior porque un
    /// health check que consulta la base es mas lento y no conviene usarlo como
    /// sonda de disponibilidad.
    /// </summary>
    [HttpGet("db")]
    public async Task<IActionResult> BaseDeDatos(CancellationToken ct)
    {
        var conecta = await _context.Database.CanConnectAsync(ct);

        if (!conecta)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { estado = "sin conexion a la base de datos" });
        }

        var pendientes = await _context.Database.GetPendingMigrationsAsync(ct);

        return Ok(new
        {
            estado = "ok",
            baseDeDatos = "conectada",
            migracionesPendientes = pendientes.ToArray()
        });
    }
}
