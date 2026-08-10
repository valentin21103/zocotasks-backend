using ZocoTasks.Domain.Enums;
using ZocoTasks.Domain.Exceptions;

namespace ZocoTasks.Domain.Common;

/// <summary>
/// Reglas del pipeline comercial. Vive en Domain porque es la regla de negocio
/// central del sistema, y por eso mismo es directamente testeable sin base ni
/// contenedor de dependencias.
///
/// El embudo tiene un orden natural (Nuevo, Contactado, Interesado,
/// Documentacion, Aprobado, Rechazado) pero el movimiento entre estados es
/// libre: se puede avanzar, saltear etapas, corregir hacia atras y reabrir una
/// oportunidad ya cerrada. La unica transicion invalida es la de un estado a si
/// mismo, porque no es un cambio.
///
/// Decision de negocio, tomada a conciencia: un pipeline rigido dejaba trabado
/// al vendedor que cargaba mal un estado, sin ninguna forma de corregirlo. El
/// costo asumido es que el sistema ya no impide un salto que comercialmente no
/// tendria sentido, como aprobar un comercio que nadie contacto.
///
/// <see cref="EsFinal"/> sigue existiendo, pero como *clasificacion* y no como
/// restriccion: alimenta la columna <c>es_final</c> del catalogo y sirve para
/// reportar cuantas oportunidades estan cerradas. Ya no bloquea la salida.
/// </summary>
public static class MaquinaEstadoComercio
{
    /// <summary>
    /// Estados que cierran la oportunidad. Es una etiqueta para reportes: no
    /// impide moverse, porque una oportunidad cerrada puede reabrirse.
    /// </summary>
    public static bool EsFinal(EstadoComercioEnum estado) =>
        estado is EstadoComercioEnum.Aprobado or EstadoComercioEnum.Rechazado;

    /// <summary>Posicion del estado en el embudo, usada para ordenar reportes.</summary>
    public static short Orden(EstadoComercioEnum estado) => (short)estado;

    /// <summary>
    /// Todos los estados menos el actual. El frontend arma con esto el selector,
    /// asi que la regla sigue viviendo de un solo lado: si algun dia se vuelve a
    /// restringir, la interfaz se adapta sin cambios.
    /// </summary>
    public static IReadOnlyCollection<EstadoComercioEnum> TransicionesDesde(EstadoComercioEnum desde) =>
        [.. Enum.GetValues<EstadoComercioEnum>().Where(estado => estado != desde)];

    public static bool PuedeTransicionar(EstadoComercioEnum desde, EstadoComercioEnum hacia) =>
        Enum.IsDefined(hacia) && desde != hacia;

    /// <summary>
    /// Valida la transicion o lanza. Se usa desde <c>Comercio.CambiarEstado</c>
    /// para que ningun camino de codigo pueda saltearse la regla.
    /// </summary>
    public static void ValidarTransicion(EstadoComercioEnum desde, EstadoComercioEnum hacia)
    {
        if (!PuedeTransicionar(desde, hacia))
        {
            throw new EstadoTransicionInvalidaException(desde, hacia);
        }
    }
}
