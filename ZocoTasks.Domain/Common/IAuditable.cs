namespace ZocoTasks.Domain.Common;

/// <summary>
/// Marca una entidad cuyas fechas de alta y modificacion las mantiene la
/// infraestructura (interceptor de SaveChanges), no el codigo de aplicacion.
/// </summary>
public interface IAuditable
{
    DateTime FechaCreacion { get; set; }
    DateTime? FechaActualizacion { get; set; }
}
