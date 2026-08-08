using ZocoTasks.Domain.Common;

namespace ZocoTasks.Domain.Entities;

/// <summary>
/// Rol de aplicacion. Se proyecta como claim en el JWT.
/// Sembrado por migracion: 1 Admin, 2 Vendedor.
/// </summary>
public class Rol : EntidadBase
{
    public string Nombre { get; set; } = null!;

    public ICollection<UsuarioRol> UsuarioRoles { get; set; } = new List<UsuarioRol>();
}
