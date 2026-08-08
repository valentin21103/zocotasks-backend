using ZocoTasks.Domain.Enums;

namespace ZocoTasks.Domain.Entities;

/// <summary>
/// Catalogo de tipos de interaccion. Sembrado por migracion.
/// </summary>
public class TipoInteraccion
{
    public TipoInteraccionEnum Id { get; set; }

    public string Codigo { get; set; } = null!;

    public string Nombre { get; set; } = null!;

    public ICollection<Interaccion> Interacciones { get; set; } = new List<Interaccion>();
}
