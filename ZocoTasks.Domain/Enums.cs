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

/// <summary>
/// Tipos de interaccion registrables contra un comercio.
/// A diferencia del estado, la consigna plantea esta lista como abierta
/// ("por ejemplo: llamada, WhatsApp..."), asi que la tabla <c>tipo_interaccion</c>
/// admite altas sin tocar el codigo. El enum cubre los tipos conocidos.
/// </summary>
public enum TipoInteraccionEnum : short
{
    Llamada = 1,
    WhatsApp = 2,
    Reunion = 3,
    Email = 4,
    NotaInterna = 5
}

/// <summary>
/// Nivel de interes estimado por la funcion "Analizar oportunidad".
/// <see cref="Indeterminado"/> es el valor de la respuesta degradada: cuando el
/// proveedor de IA falla no se inventa un nivel.
/// </summary>
public enum NivelInteres : short
{
    Indeterminado = 0,
    Bajo = 1,
    Medio = 2,
    Alto = 3
}
