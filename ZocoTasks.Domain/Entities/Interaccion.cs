using ZocoTasks.Domain.Common;
using ZocoTasks.Domain.Enums;

namespace ZocoTasks.Domain.Entities;

/// <summary>
/// Contacto registrado contra un comercio: llamada, WhatsApp, reunion, email o
/// nota interna. Segunda entidad del modelo, 1:N con <see cref="Comercio"/>.
/// </summary>
public class Interaccion : EntidadBase
{
    public int ComercioId { get; set; }
    public Comercio Comercio { get; set; } = null!;

    public TipoInteraccionEnum Tipo { get; set; }
    public TipoInteraccion TipoNavegacion { get; set; } = null!;

    /// <summary>Quien registro la interaccion. Nulo si el usuario fue dado de baja.</summary>
    public int? UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }

    /// <summary>Cuando ocurrio el contacto (puede ser anterior a la carga).</summary>
    public DateTime Fecha { get; set; }

    public string Detalle { get; set; } = null!;

    /// <summary>Cuando se cargo en el sistema.</summary>
    public DateTime FechaCreacion { get; set; }
}
