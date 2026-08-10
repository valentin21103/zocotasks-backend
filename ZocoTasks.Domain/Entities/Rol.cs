namespace ZocoTasks.Domain.Entities;

/// <summary>
/// Sembrado por migracion: 1 Admin, 2 Vendedor.
/// </summary>
public class Rol
{
    public int Id { get; set; }

    public string Nombre { get; set; } = null!;

    public ICollection<UsuarioRol> UsuarioRoles { get; set; } = new List<UsuarioRol>();
}
