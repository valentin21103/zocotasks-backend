using ZocoTasks.Domain.Common;

namespace ZocoTasks.Domain.Entities;

/// <summary>
/// Rubro comercial. A diferencia del estado no tiene enum asociado: los rubros
/// cambian sin que cambie el codigo, asi que es una tabla con ABM. El flag
/// <see cref="Activo"/> permite dar de baja un rubro sin romper los comercios
/// historicos que ya lo referencian.
/// </summary>
public class Rubro : EntidadBase
{
    public string Nombre { get; set; } = null!;

    public bool Activo { get; set; } = true;

    public ICollection<Comercio> Comercios { get; set; } = new List<Comercio>();
}
