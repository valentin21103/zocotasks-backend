namespace ZocoTasks.Domain.Common;

/// <summary>
/// Entidad que se da de baja logicamente. El borrado fisico se llevaria puestas
/// las interacciones y el historial, que son justamente la evidencia del
/// seguimiento comercial.
/// </summary>
public interface ISoftDelete
{
    DateTime? FechaEliminacion { get; set; }
}
