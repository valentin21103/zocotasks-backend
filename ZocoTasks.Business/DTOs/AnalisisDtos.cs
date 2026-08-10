using ZocoTasks.Domain.Enums;

namespace ZocoTasks.Business.DTOs;

/// <summary>
/// Resultado de "Analizar oportunidad".
/// </summary>

public class AnalisisOportunidadDto
{
    public string Resumen { get; set; } = string.Empty;

    public NivelInteres NivelInteres { get; set; }

    public string ProximoPaso { get; set; } = string.Empty;

    public IReadOnlyList<string> PreguntasSugeridas { get; set; } = [];

    public IReadOnlyList<string> DatosFaltantes { get; set; } = [];

    /// <summary>
    /// true cuando el proveedor de IA fallo y esto es una respuesta degradada.
    /// El frontend tiene que avisarlo en lugar de presentarlo como un analisis
    /// valido: si el modelo no respondio, el sistema lo dice.
    /// </summary>
    public bool EsDegradado { get; set; }

    public string ModeloUtilizado { get; set; } = string.Empty;

    public DateTime FechaGeneracion { get; set; }
}
