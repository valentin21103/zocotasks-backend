using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using ZocoTasks.Business.DTOs;
using ZocoTasks.Business.Interfaces;

namespace ZocoTasks.Business.Services;

public class AuthService : IAuthService
{
    /// <summary>
    /// Sin refresh token: el usuario se loguea una vez y trabaja toda la
    /// jornada. Un refresh necesitaria tabla, rotacion y revocacion, que es
    /// mucha maquinaria para el alcance de esta prueba.
    /// </summary>
    private const int HorasDeVigencia = 12;

    private readonly IUsuarioRepository _repository;
    private readonly string _clave;

    public AuthService(IUsuarioRepository repository, IConfiguration configuration)
    {
        _repository = repository;

        _clave = configuration["Jwt:Clave"]
            ?? throw new InvalidOperationException(
                "Falta Jwt:Clave. Definirla en user-secrets o en la variable de " +
                "entorno Jwt__Clave. Ver .env.example.");
    }

    public async Task<LoginRespuestaDto?> Login(LoginDto dto, CancellationToken ct)
    {
        var usuario = await _repository.BuscarPorEmail(dto.Email, ct);

        // Se devuelve null en los tres casos (no existe, contrasenia mal,
        // usuario inactivo) y el controller responde siempre el mismo mensaje.
        // Distinguirlos le diria a un atacante que emails estan registrados.
        if (usuario is null || !usuario.Activo)
        {
            return null;
        }

        if (!BCrypt.Net.BCrypt.Verify(dto.Password, usuario.PasswordHash))
        {
            return null;
        }

        var roles = usuario.UsuarioRoles.Select(ur => ur.Rol.Nombre).ToList();
        var expira = DateTime.UtcNow.AddHours(HorasDeVigencia);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new(ClaimTypes.Email, usuario.Email),
            new(ClaimTypes.Name, usuario.NombreCompleto)
        };

        // Un claim de rol por cada rol. Esto es lo que hace funcionar
        // [Authorize(Roles = "Admin")] en los controllers.
        claims.AddRange(roles.Select(rol => new Claim(ClaimTypes.Role, rol)));

        var credenciales = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_clave)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            claims: claims,
            expires: expira,
            signingCredentials: credenciales);

        return new LoginRespuestaDto
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            ExpiraEn = expira,
            Email = usuario.Email,
            NombreCompleto = usuario.NombreCompleto,
            Roles = roles
        };
    }
}
