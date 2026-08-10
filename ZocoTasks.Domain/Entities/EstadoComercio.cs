using ZocoTasks.Domain.Enums;

namespace ZocoTasks.Domain.Entities;

/// <summary>
/// Catalogo de estados del pipeline. La PK es el propio enum: la base guarda el
/// </summary>
public class EstadoComercio
{
    public EstadoComercioEnum Id { get; set; }

    /// <summary>Clave estable para el front y los reportes (no se traduce).</summary>
    public string Codigo { get; set; } = null!;

    public string Nombre { get; set; } = null!;

    /// <summary>Posicion en el embudo, para ordenar.</summary>
    public short Orden { get; set; }

    /// <summary>Aprobado y Rechazado son terminales.</summary>
    public bool EsFinal { get; set; }

    public ICollection<Comercio> Comercios { get; set; } = new List<Comercio>();
}
