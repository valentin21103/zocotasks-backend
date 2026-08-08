namespace ZocoTasks.Domain.Enums;

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
