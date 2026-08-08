namespace ZocoTasks.Domain.Exceptions;

/// <summary>
/// Una regla de negocio impide completar la operacion: por ejemplo, un CUIT ya
/// registrado o un rubro dado de baja.
/// </summary>
/// <remarks>
/// Se distingue de una falla de validacion de formato (que la resuelve
/// FluentValidation y devuelve 400 con el detalle por campo) porque estas
/// reglas solo pueden verificarse consultando la base. Se traduce a
/// 422 Unprocessable Entity: la peticion esta bien formada, pero el estado
/// actual del sistema no permite procesarla.
/// </remarks>
public sealed class ReglaDeNegocioException : DomainException
{
    public ReglaDeNegocioException(string mensaje) : base(mensaje) { }

    public override string Codigo => "regla_de_negocio";
}
