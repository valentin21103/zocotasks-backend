namespace ZocoTasks.Domain.Entities;

/// <summary>
/// Registro de auditoria. Lo escribe el interceptor de SaveChanges, de forma
/// generica para toda entidad rastreada: ningun servicio lo llena a mano.
/// </summary>
/// <remarks>
/// Su PK es <see cref="long"/> y no <see cref="int"/>: es la tabla que mas
/// rapido crece del modelo.
/// </remarks>
public class AuditLog
{
    public long Id { get; set; }

    /// <summary>Nombre de la entidad afectada, por ejemplo "Comercio".</summary>
    public string Entidad { get; set; } = null!;

    /// <summary>PK de la fila afectada, como texto para admitir cualquier tipo de clave.</summary>
    public string EntidadId { get; set; } = null!;

    /// <summary>Insert, Update o Delete.</summary>
    public string Accion { get; set; } = null!;

    public int? UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }

    public DateTime Fecha { get; set; }

    /// <summary>
    /// Diccionario JSON con los valores anterior y nuevo de cada propiedad
    /// modificada. Va en <c>jsonb</c>: la forma cambia segun la entidad.
    /// </summary>
    public string? Cambios { get; set; }
}
