namespace ZocoTasks.Domain.Exceptions;

/// <summary>
/// La entidad pedida no existe o esta dada de baja logicamente.
/// Se traduce a 404 Not Found.
/// </summary>
public sealed class EntidadNoEncontradaException : DomainException
{
    public EntidadNoEncontradaException(string entidad, object id)
        : base($"No se encontro {entidad} con id {id}.")
    {
        Entidad = entidad;
        Id = id;
    }

    public string Entidad { get; }
    public object Id { get; }

    public override string Codigo => "entidad_no_encontrada";
}
