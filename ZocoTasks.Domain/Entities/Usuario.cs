using ZocoTasks.Domain.Common;

namespace ZocoTasks.Domain.Entities;

/// <summary>
/// Usuario del backoffice comercial.
/// </summary>
public class Usuario : EntidadBase, IAuditable
{
    /// <summary>
    /// Se persiste como <c>citext</c>: la comparacion case-insensitive la hace
    /// el tipo de Postgres, de modo que el indice unico ya rechaza
    /// "Juan@mail.com" contra "juan@mail.com" sin normalizar en cada insert.
    /// </summary>
    public string Email { get; set; } = null!;

    /// <summary>Hash BCrypt. Nunca sale en un DTO.</summary>
    public string PasswordHash { get; set; } = null!;

    public string NombreCompleto { get; set; } = null!;

    public bool Activo { get; set; } = true;

    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaActualizacion { get; set; }

    public ICollection<UsuarioRol> UsuarioRoles { get; set; } = new List<UsuarioRol>();

    /// <summary>Comercios de los que este usuario es responsable comercial.</summary>
    public ICollection<Comercio> ComerciosAsignados { get; set; } = new List<Comercio>();
}
