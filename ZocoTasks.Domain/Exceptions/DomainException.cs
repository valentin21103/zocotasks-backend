namespace ZocoTasks.Domain.Exceptions;

/// <summary>
/// Raiz de las excepciones de negocio. El middleware de la API las traduce a
/// ProblemDetails con codigo HTTP acorde; cualquier otra excepcion es un 500.
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string mensaje) : base(mensaje) { }

    /// <summary>Identificador estable para que el front discrimine el error.</summary>
    public abstract string Codigo { get; }
}
