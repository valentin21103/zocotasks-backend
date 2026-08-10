namespace ZocoTasks.Business.DTOs;

/// <summary>
/// Rubro visto desde la pantalla de administracion.
/// </summary>
/// <remarks>
/// A diferencia de <see cref="CatalogoItemDto"/>, que alimenta el combo del
/// formulario y solo trae los activos, este DTO trae tambien los dados de baja
/// y la cantidad de comercios que lo usan. Esa cuenta es lo que permite avisarle
/// al usuario, antes de que apriete, que borrar el rubro no va a ser una baja
/// fisica.
/// </remarks>
public class RubroAbmDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public bool Activo { get; set; }
    public int CantidadComercios { get; set; }
}

public class GuardarRubroDto
{
    public string Nombre { get; set; } = string.Empty;

    /// <summary>
    /// Permite reactivar un rubro dado de baja sin un endpoint aparte.
    /// En el alta siempre queda activo.
    /// </summary>
    public bool Activo { get; set; } = true;
}

/// <summary>
/// Que paso al eliminar. El endpoint devuelve esto en vez de un 204 pelado
/// porque la baja tiene dos comportamientos y el usuario merece saber cual
/// ocurrio.
/// </summary>
public class ResultadoBajaRubroDto
{
    /// <summary>true: se borro de la base. false: quedo desactivado.</summary>
    public bool Eliminado { get; set; }

    public int ComerciosAsociados { get; set; }
}
