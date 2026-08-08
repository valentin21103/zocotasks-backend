namespace ZocoTasks.Domain.Common;

/// <summary>
/// Base de las entidades con clave primaria entera autogenerada.
/// No incluye fechas: no todas las tablas las necesitan (los catalogos, por
/// ejemplo). Eso lo aporta <see cref="IAuditable"/>.
/// </summary>
public abstract class EntidadBase
{
    public int Id { get; set; }
}
