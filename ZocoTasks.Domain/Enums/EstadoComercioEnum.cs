namespace ZocoTasks.Domain.Enums;

/// <summary>
/// Estados del pipeline comercial. Se persiste como smallint con FK real contra
/// la tabla <c>estado_comercio</c>: la maquina de estados es logica de dominio,
/// pero la integridad referencial la garantiza la base.
/// </summary>
public enum EstadoComercioEnum : short
{
    Nuevo = 1,
    Contactado = 2,
    Interesado = 3,
    Documentacion = 4,
    Aprobado = 5,
    Rechazado = 6
}
