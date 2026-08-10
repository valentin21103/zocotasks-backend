namespace ZocoTasks.Domain.Entities;

/// <summary>
/// Usuario del backoffice comercial.
/// </summary>
public class Usuario
{
    public int Id { get; set; }

    public string Email { get; set; } = null!;

    /// <summary>Hash BCrypt. Nunca sale en un DTO.</summary>
    public string PasswordHash { get; set; } = null!;

    public string NombreCompleto { get; set; } = null!;

    public bool Activo { get; set; } = true;

    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaActualizacion { get; set; }

    public ICollection<UsuarioRol> UsuarioRoles { get; set; } = new List<UsuarioRol>();

    public ICollection<Comercio> ComerciosAsignados { get; set; } = new List<Comercio>();
}
