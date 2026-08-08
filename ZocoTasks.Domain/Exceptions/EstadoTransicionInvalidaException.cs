using ZocoTasks.Domain.Enums;

namespace ZocoTasks.Domain.Exceptions;

/// <summary>
/// Se intento mover un comercio a un estado que el pipeline no permite desde su
/// estado actual. Se traduce a 409 Conflict.
/// </summary>
public sealed class EstadoTransicionInvalidaException : DomainException
{
    public EstadoTransicionInvalidaException(EstadoComercioEnum desde, EstadoComercioEnum hacia)
        : base($"No se puede pasar de '{desde}' a '{hacia}'.")
    {
        Desde = desde;
        Hacia = hacia;
    }

    public EstadoComercioEnum Desde { get; }
    public EstadoComercioEnum Hacia { get; }

    public override string Codigo => "estado_transicion_invalida";
}
