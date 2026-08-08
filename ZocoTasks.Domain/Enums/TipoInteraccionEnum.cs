namespace ZocoTasks.Domain.Enums;

/// <summary>
/// Tipos de interaccion registrables contra un comercio. Mismo criterio que
/// <see cref="EstadoComercioEnum"/>: enum en codigo, tabla lookup en base.
/// </summary>
public enum TipoInteraccionEnum : short
{
    Llamada = 1,
    WhatsApp = 2,
    Reunion = 3,
    Email = 4,
    NotaInterna = 5
}
