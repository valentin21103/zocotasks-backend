using ZocoTasks.Domain.Enums;
using ZocoTasks.Domain.Exceptions;

namespace ZocoTasks.Domain.Common;

/// <summary>
/// Reglas del pipeline comercial. Vive en Domain porque es la regla de negocio
/// central del sistema, y por eso mismo es directamente testeable sin base ni
/// contenedor de dependencias.
///
/// El pipeline es lineal y no admite retrocesos: cada estado avanza unicamente
/// al siguiente. "Rechazado" es la salida disponible desde cualquier estado no
/// terminal, porque un comercio puede caerse en cualquier punto del embudo.
/// "Aprobado" y "Rechazado" son terminales.
/// </summary>
public static class MaquinaEstadoComercio
{
    private static readonly IReadOnlyDictionary<EstadoComercioEnum, EstadoComercioEnum[]> Permitidas =
        new Dictionary<EstadoComercioEnum, EstadoComercioEnum[]>
        {
            [EstadoComercioEnum.Nuevo] = [EstadoComercioEnum.Contactado, EstadoComercioEnum.Rechazado],
            [EstadoComercioEnum.Contactado] = [EstadoComercioEnum.Interesado, EstadoComercioEnum.Rechazado],
            [EstadoComercioEnum.Interesado] = [EstadoComercioEnum.Documentacion, EstadoComercioEnum.Rechazado],
            [EstadoComercioEnum.Documentacion] = [EstadoComercioEnum.Aprobado, EstadoComercioEnum.Rechazado],
            [EstadoComercioEnum.Aprobado] = [],
            [EstadoComercioEnum.Rechazado] = []
        };

    /// <summary>Estados terminales: no admiten ninguna transicion de salida.</summary>
    public static bool EsFinal(EstadoComercioEnum estado) =>
        estado is EstadoComercioEnum.Aprobado or EstadoComercioEnum.Rechazado;

    /// <summary>Posicion del estado en el embudo, usada para ordenar reportes.</summary>
    public static short Orden(EstadoComercioEnum estado) => (short)estado;

    public static IReadOnlyCollection<EstadoComercioEnum> TransicionesDesde(EstadoComercioEnum desde) =>
        Permitidas.TryGetValue(desde, out var destinos) ? destinos : [];

    public static bool PuedeTransicionar(EstadoComercioEnum desde, EstadoComercioEnum hacia) =>
        Permitidas.TryGetValue(desde, out var destinos) && destinos.Contains(hacia);

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
