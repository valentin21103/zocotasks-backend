using ZocoTasks.Domain.Common;
using ZocoTasks.Domain.Enums;

namespace ZocoTasks.Domain.Entities;

/// <summary>
/// Resultado persistido de "Analizar oportunidad".
/// </summary>
/// <remarks>
/// Se guarda en lugar de devolverse al vuelo por tres razones:
/// cacheo por <see cref="HashContexto"/> (si el contexto no cambio se devuelve
/// el analisis previo en vez de volver a pagar tokens), trazabilidad de como
/// evoluciono el interes del comercio a lo largo del tiempo, y control de costo.
/// </remarks>
public class AnalisisOportunidad : EntidadBase
{
    public int ComercioId { get; set; }
    public Comercio Comercio { get; set; } = null!;

    public NivelInteres NivelInteres { get; set; }

    public string Resumen { get; set; } = null!;

    public string ProximoPaso { get; set; } = null!;

    /// <summary>
    /// Las tres preguntas sugeridas al vendedor. Va en <c>jsonb</c>: es un array
    /// de solo lectura sobre el que no se hacen consultas relacionales, asi que
    /// normalizarlo en una tabla aparte seria sobreingenieria.
    /// </summary>
    public List<string> PreguntasSugeridas { get; set; } = [];

    /// <summary>Datos que faltan para calificar la oportunidad. Tambien <c>jsonb</c>.</summary>
    public List<string> DatosFaltantes { get; set; } = [];

    public string ModeloUtilizado { get; set; } = null!;

    /// <summary>
    /// SHA256 en hexadecimal (64 caracteres) del contexto enviado al modelo.
    /// Es la clave de cache: mismo contexto, mismo analisis.
    /// </summary>
    public string HashContexto { get; set; } = null!;

    public DateTime FechaGeneracion { get; set; }

    public int? UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }

    /// <summary>
    /// Marca la respuesta degradada que se devuelve cuando el proveedor de IA
    /// falla. Nunca se cachea ni se presenta como analisis valido.
    /// </summary>
    public bool EsDegradado { get; set; }
}
