using ZocoTasks.Domain.Common;
using ZocoTasks.Domain.Enums;

namespace ZocoTasks.Domain.Entities;

/// <summary>
/// Una transicion del pipeline. Se escribe siempre desde
/// <see cref="Comercio.CambiarEstado"/>, nunca a mano.
/// </summary>
/// <remarks>
/// Ademas de cubrir trazabilidad, esta tabla es la que le da señal temporal a
/// "Analizar oportunidad": permite saber hace cuanto que el comercio esta
/// trabado en el estado actual, que es justamente el dato que un texto plano de
/// notas no puede aportar.
/// </remarks>
public class HistorialEstado : EntidadBase
{
    public int ComercioId { get; set; }
    public Comercio Comercio { get; set; } = null!;

    /// <summary>Nulo unicamente en el registro de alta del comercio.</summary>
    public EstadoComercioEnum? EstadoAnterior { get; set; }

    public EstadoComercioEnum EstadoNuevo { get; set; }

    public int? UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }

    public DateTime Fecha { get; set; }

    public string? Motivo { get; set; }
}
