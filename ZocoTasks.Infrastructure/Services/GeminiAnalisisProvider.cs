using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ZocoTasks.Business.DTOs;
using ZocoTasks.Business.Interfaces;
using ZocoTasks.Domain.Enums;

namespace ZocoTasks.Infrastructure.Services;

/// <summary>
/// Implementacion de <see cref="IProveedorAnalisisIA"/> contra Google Gemini.
/// </summary>
/// <remarks>
/// Se eligio Gemini Flash por el free tier: alcanza de sobra para la prueba y
/// no hay que poner tarjeta. La respuesta viene como JSON estructurado, asi que
/// no hay que interpretar texto libre.
/// </remarks>
public class GeminiAnalisisProvider : IProveedorAnalisisIA
{
    /// <summary>
    /// Verificado contra la cuenta del proyecto: es el modelo que tiene cuota
    /// en el free tier. Los gemini-*-flash a secas devuelven 429 con "limit: 0",
    /// que no significa cuota agotada sino que este proyecto no tiene
    /// asignacion gratuita para ellos. Se puede cambiar con GEMINI_MODELO.
    /// </summary>
    private const string ModeloPorDefecto = "gemini-flash-lite-latest";

    private static readonly JsonSerializerOptions OpcionesJson = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _http;
    private readonly ILogger<GeminiAnalisisProvider> _logger;
    private readonly string? _apiKey;

    public GeminiAnalisisProvider(
        HttpClient http,
        IConfiguration configuration,
        ILogger<GeminiAnalisisProvider> logger)
    {
        _http = http;
        _logger = logger;

        // A diferencia de la cadena de conexion, la falta de esta clave NO
        // impide arrancar: sin ella la aplicacion funciona entera y solo el
        // analisis responde degradado.
        _apiKey = configuration["GEMINI_API_KEY"];

        Modelo = configuration["GEMINI_MODELO"] ?? ModeloPorDefecto;
    }

    public string Modelo { get; }

    public async Task<AnalisisOportunidadDto> Analizar(
        string instruccionSistema, string contexto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            throw new InvalidOperationException(
                "Falta GEMINI_API_KEY. Definirla en user-secrets o como variable " +
                "de entorno. Ver .env.example.");
        }

        var peticion = new
        {
            systemInstruction = new { parts = new[] { new { text = instruccionSistema } } },
            contents = new[]
            {
                new { role = "user", parts = new[] { new { text = contexto } } }
            },
            generationConfig = new
            {
                // Lo unico que se configura, y es lo que hace confiable la
                // respuesta: Gemini garantiza que el JSON valida contra el
                // esquema, en vez de pedirselo por prompt y cruzar los dedos.
                // El resto de los parametros quedan en sus valores por defecto.
                responseMimeType = "application/json",
                responseSchema = EsquemaRespuesta()
            }
        };

        using var mensaje = new HttpRequestMessage(
            HttpMethod.Post, $"v1beta/models/{Modelo}:generateContent")
        {
            Content = JsonContent.Create(peticion)
        };

        // La clave va en un header y no en la query string: las URLs quedan en
        // los logs de servidores y proxies, los headers no.
        mensaje.Headers.Add("x-goog-api-key", _apiKey);

        using var respuesta = await _http.SendAsync(mensaje, ct);

        if (!respuesta.IsSuccessStatusCode)
        {
            var cuerpo = await respuesta.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException(
                $"Gemini respondio {(int)respuesta.StatusCode}: {Recortar(cuerpo)}");
        }

        var json = await respuesta.Content.ReadAsStringAsync(ct);

        return Interpretar(json);
    }

    private AnalisisOportunidadDto Interpretar(string json)
    {
        using var documento = JsonDocument.Parse(json);

        if (!documento.RootElement.TryGetProperty("candidates", out var candidatos)
            || candidatos.GetArrayLength() == 0)
        {
            // Pasa cuando los filtros de seguridad bloquean la respuesta.
            throw new InvalidOperationException(
                $"Gemini no devolvio ningun candidato. Respuesta: {Recortar(json)}");
        }

        var texto = candidatos[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();

        if (string.IsNullOrWhiteSpace(texto))
        {
            throw new InvalidOperationException("Gemini devolvio un contenido vacio.");
        }

        var cruda = JsonSerializer.Deserialize<RespuestaGemini>(texto, OpcionesJson)
            ?? throw new InvalidOperationException(
                $"No se pudo interpretar el JSON de Gemini: {Recortar(texto)}");

        if (!Enum.TryParse<NivelInteres>(cruda.NivelInteres, ignoreCase: true, out var nivel))
        {
            _logger.LogWarning(
                "Gemini devolvio un nivel de interes desconocido: {Nivel}", cruda.NivelInteres);
            nivel = NivelInteres.Indeterminado;
        }

        var preguntas = cruda.PreguntasSugeridas ?? [];

        if (preguntas.Count != 3)
        {
            // El esquema pide tres. Si llegan mas se recortan; si llegan menos
            // se deja constancia pero no se inventan: es preferible mostrar dos
            // preguntas reales que tres con una fabricada.
            _logger.LogWarning(
                "Gemini devolvio {Cantidad} preguntas en lugar de 3", preguntas.Count);
        }

        return new AnalisisOportunidadDto
        {
            Resumen = cruda.Resumen ?? string.Empty,
            NivelInteres = nivel,
            ProximoPaso = cruda.ProximoPaso ?? string.Empty,
            PreguntasSugeridas = [.. preguntas.Take(3)],
            DatosFaltantes = cruda.DatosFaltantes ?? [],
            EsDegradado = false
        };
    }

    /// <summary>
    /// Esquema que Gemini usa para forzar la forma de la respuesta.
    /// Es un subconjunto de OpenAPI: los tipos van en mayusculas.
    /// </summary>
    private static object EsquemaRespuesta() => new
    {
        type = "OBJECT",
        properties = new
        {
            resumen = new { type = "STRING" },
            nivelInteres = new
            {
                type = "STRING",
                @enum = new[] { "Alto", "Medio", "Bajo", "Indeterminado" }
            },
            proximoPaso = new { type = "STRING" },
            preguntasSugeridas = new
            {
                type = "ARRAY",
                items = new { type = "STRING" },
                minItems = 3,
                maxItems = 3
            },
            datosFaltantes = new
            {
                type = "ARRAY",
                items = new { type = "STRING" }
            }
        },
        required = new[]
        {
            "resumen", "nivelInteres", "proximoPaso", "preguntasSugeridas", "datosFaltantes"
        },
        // Sin esto el orden de las claves puede variar entre llamadas y el
        // modelo a veces razona peor. Es una recomendacion de la documentacion.
        propertyOrdering = new[]
        {
            "resumen", "nivelInteres", "proximoPaso", "preguntasSugeridas", "datosFaltantes"
        }
    };

    /// <summary>Recorta para que un error del proveedor no llene el log.</summary>
    private static string Recortar(string texto) =>
        texto.Length <= 500 ? texto : texto[..500] + "…";

    private sealed class RespuestaGemini
    {
        [JsonPropertyName("resumen")]
        public string? Resumen { get; set; }

        [JsonPropertyName("nivelInteres")]
        public string? NivelInteres { get; set; }

        [JsonPropertyName("proximoPaso")]
        public string? ProximoPaso { get; set; }

        [JsonPropertyName("preguntasSugeridas")]
        public List<string>? PreguntasSugeridas { get; set; }

        [JsonPropertyName("datosFaltantes")]
        public List<string>? DatosFaltantes { get; set; }
    }
}
