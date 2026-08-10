using ZocoTasks.Business.DTOs;

namespace ZocoTasks.Business.Interfaces;

public interface IAuthService
{
    /// <summary>
    /// Valida las credenciales y devuelve el token. Null si el email no existe,
    /// la contrasenia no coincide o el usuario esta dado de baja.
    /// </summary>
    Task<LoginRespuestaDto?> Login(LoginDto dto, CancellationToken ct);
}
