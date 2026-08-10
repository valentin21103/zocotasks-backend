using ZocoTasks.Domain.Entities;
using ZocoTasks.Domain.Enums;

namespace ZocoTasks.Infrastructure.Data;

/// <summary>
/// Datos de catalogo que viajan dentro de la migracion (via <c>HasData</c>), no
/// en un script aparte: asi una base recien creada queda consistente con solo
/// correr <c>dotnet ef database update</c>.
/// </summary>
public static class CatalogosSeed
{
    private static readonly Dictionary<EstadoComercioEnum, string> NombresEstado = new()
    {
        [EstadoComercioEnum.Nuevo] = "Nuevo",
        [EstadoComercioEnum.Contactado] = "Contactado",
        [EstadoComercioEnum.Interesado] = "Interesado",
        [EstadoComercioEnum.Documentacion] = "Documentación",
        [EstadoComercioEnum.Aprobado] = "Aprobado",
        [EstadoComercioEnum.Rechazado] = "Rechazado"
    };

    private static readonly Dictionary<TipoInteraccionEnum, string> NombresTipo = new()
    {
        [TipoInteraccionEnum.Llamada] = "Llamada",
        [TipoInteraccionEnum.WhatsApp] = "WhatsApp",
        [TipoInteraccionEnum.Reunion] = "Reunión",
        [TipoInteraccionEnum.Email] = "Email",
        [TipoInteraccionEnum.NotaInterna] = "Nota interna"
    };

    /// <summary>Los estados se derivan del enum en lugar de repetirse a mano.</summary>
    public static EstadoComercio[] Estados =>
        Enum.GetValues<EstadoComercioEnum>()
            .Select(e => new EstadoComercio
            {
                Id = e,
                Codigo = e.ToString(),
                Nombre = NombresEstado[e],
                Orden = (short)e,
                EsFinal = e is EstadoComercioEnum.Aprobado or EstadoComercioEnum.Rechazado
            })
            .ToArray();

    public static TipoInteraccion[] TiposInteraccion =>
        Enum.GetValues<TipoInteraccionEnum>()
            .Select(t => new TipoInteraccion
            {
                Id = t,
                Codigo = t.ToString(),
                Nombre = NombresTipo[t]
            })
            .ToArray();

    /// <summary>
    /// Carga inicial de rubros. A diferencia de los estados, esta tabla tiene
    /// ABM: estos son solo los valores con los que arranca el sistema.
    /// </summary>
    public static Rubro[] Rubros =>
    [
        new() { Id = 1, Nombre = "Gastronomía", Activo = true },
        new() { Id = 2, Nombre = "Indumentaria", Activo = true },
        new() { Id = 3, Nombre = "Kiosco y autoservicio", Activo = true },
        new() { Id = 4, Nombre = "Salud y estética", Activo = true },
        new() { Id = 5, Nombre = "Servicios profesionales", Activo = true },
        new() { Id = 6, Nombre = "Tecnología", Activo = true },
        new() { Id = 7, Nombre = "Transporte y logística", Activo = true },
        new() { Id = 8, Nombre = "Educación", Activo = true },
        new() { Id = 9, Nombre = "Otros", Activo = true }
    ];

    public static Rol[] Roles =>
    [
        new() { Id = 1, Nombre = "Admin" },
        new() { Id = 2, Nombre = "Vendedor" }
    ];
}
