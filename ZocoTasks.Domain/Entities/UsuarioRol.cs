namespace ZocoTasks.Domain.Entities;

/// <summary>
/// Tabla puente de la relacion N:M entre usuario y rol.
/// Se modela explicitamente (en lugar de dejar que EF la genere) porque asi la
/// PK compuesta y las FK quedan declaradas y versionadas en la migracion.
/// </summary>
public class UsuarioRol
{
    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;

    public int RolId { get; set; }
    public Rol Rol { get; set; } = null!;
}
