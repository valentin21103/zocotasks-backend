namespace ZocoTasks.Business.DTOs;

public class LoginDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginRespuestaDto
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiraEn { get; set; }
    public string Email { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;

    /// <summary>El front usa esto para mostrar u ocultar el boton de eliminar.</summary>
    public IReadOnlyList<string> Roles { get; set; } = [];
}
