namespace ZocoTasks.Domain.Exceptions;

/// <summary>
/// La operacion exige una precondicion que el cliente no envio: en la practica,
/// el header <c>If-Match</c> en un PUT o un PATCH.
/// </summary>
/// <remarks>
/// Se traduce a 428 Precondition Required (RFC 6585), que existe exactamente
/// para este caso: el servidor obliga a que la peticion sea condicional para
/// evitar el problema de "actualizacion perdida", donde dos clientes leen,
/// modifican y escriben, y el segundo pisa al primero sin que nadie se entere.
/// </remarks>
public sealed class PrecondicionRequeridaException : DomainException
{
    public PrecondicionRequeridaException(string mensaje) : base(mensaje) { }

    public override string Codigo => "precondicion_requerida";
}
