using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;
using ZocoTasks.Business.DTOs;
using ZocoTasks.Business.Interfaces;
using ZocoTasks.Domain.Entities;
using ZocoTasks.Domain.Enums;
using ZocoTasks.Domain.Exceptions;

namespace ZocoTasks.Business.Services;

public class AnalisisService : IAnalisisService
{
    /// <summary>
    /// Tope de interacciones que viajan al modelo. Las mas recientes son las
    /// que importan para evaluar la oportunidad hoy, y un comercio con cien
    /// interacciones haria un prompt enorme y caro sin aportar mas señal.
    /// </summary>
    private const int MaximoInteracciones = 20;

    private readonly IComercioRepository _repository;
    private readonly IProveedorAnalisisIA _proveedor;
    private readonly ILogger<AnalisisService> _logger;

    public AnalisisService(
        IComercioRepository repository,
        IProveedorAnalisisIA proveedor,
        ILogger<AnalisisService> logger)
    {
        _repository = repository;
        _proveedor = proveedor;
        _logger = logger;
    }

    public async Task<AnalisisOportunidadDto> Analizar(int comercioId, CancellationToken ct)
    {
        var comercio = await _repository.ObtenerConDetalle(comercioId, ct)
            ?? throw new EntidadNoEncontradaException("Comercio", comercioId);

        var contexto = ArmarContexto(comercio);

        try
        {
            var analisis = await _proveedor.Analizar(
                PromptAnalisis.InstruccionSistema, contexto, ct);

            analisis.FechaGeneracion = DateTime.UtcNow;
            analisis.ModeloUtilizado = _proveedor.Modelo;

            return analisis;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // El cliente corto la conexion: no es un fallo del proveedor y no
            // corresponde degradar, se propaga.
            throw;
        }
        catch (Exception ex)
        {
            // Que el proveedor externo falle no es una falla de esta
            // aplicacion. El resto del sistema sigue funcionando, asi que se
            // devuelve una respuesta degradada en lugar de un error: el usuario
            // se entera de que no se pudo analizar y sigue trabajando.
            _logger.LogError(ex,
                "Fallo el analisis de oportunidad del comercio {ComercioId}", comercioId);

            return Degradado();
        }
    }

    /// <summary>
    /// Respuesta cuando el proveedor no contesta.
    /// </summary>
    /// <remarks>
    /// El nivel queda en <c>Indeterminado</c> a proposito: si el modelo no
    /// respondio, el sistema no inventa un nivel de interes. Es la misma regla
    /// que se le pide al modelo, aplicada al sistema.
    /// </remarks>
    private static AnalisisOportunidadDto Degradado() => new()
    {
        Resumen = "No se pudo generar el analisis en este momento. "
                + "El servicio de IA no respondio. Volve a intentarlo en unos minutos.",
        NivelInteres = NivelInteres.Indeterminado,
        ProximoPaso = "Reintentar el analisis, o revisar manualmente las notas "
                    + "e interacciones del comercio.",
        PreguntasSugeridas = [],
        DatosFaltantes = [],
        EsDegradado = true,
        ModeloUtilizado = string.Empty,
        FechaGeneracion = DateTime.UtcNow
    };

    /// <summary>
    /// Arma el texto que se le manda al modelo con la situacion actual del
    /// comercio.
    /// </summary>
    /// <remarks>
    /// Decision de privacidad: **no se envian CUIT, telefono ni email**. Son
    /// datos personales que no aportan nada a la evaluacion comercial, y
    /// mandarlos a un tercero seria exponerlos sin necesidad. De esos campos
    /// solo viaja si estan cargados o no, que es lo unico util (le sirve al
    /// modelo para detectar datos faltantes).
    /// </remarks>
    private static string ArmarContexto(Comercio comercio)
    {
        var sb = new StringBuilder();
        var hoy = DateTime.UtcNow;

        sb.AppendLine("COMERCIO");
        sb.AppendLine($"Nombre comercial: {comercio.NombreComercial}");
        sb.AppendLine($"Rubro: {comercio.Rubro?.Nombre ?? "no especificado"}");
        sb.AppendLine($"Estado actual en el embudo: {comercio.Estado}");

        var antiguedad = (int)(hoy - comercio.FechaCreacion).TotalDays;
        sb.AppendLine($"Registrado hace: {antiguedad} dia(s)");

        sb.AppendLine($"Nombre del contacto: {comercio.NombreContacto}");
        sb.AppendLine($"Telefono cargado: {(string.IsNullOrWhiteSpace(comercio.Telefono) ? "no" : "si")}");
        sb.AppendLine($"Email cargado: {(string.IsNullOrWhiteSpace(comercio.Email) ? "no" : "si")}");

        sb.AppendLine();
        sb.AppendLine("NOTAS DEL VENDEDOR");
        sb.AppendLine(string.IsNullOrWhiteSpace(comercio.Notas)
            ? "(sin notas cargadas)"
            : comercio.Notas.Trim());

        var interacciones = comercio.Interacciones
            .OrderByDescending(i => i.Fecha)
            .Take(MaximoInteracciones)
            .ToList();

        sb.AppendLine();

        if (interacciones.Count == 0)
        {
            sb.AppendLine("INTERACCIONES");
            sb.AppendLine("(todavia no se registro ninguna interaccion con este comercio)");
            return sb.ToString();
        }

        sb.AppendLine($"INTERACCIONES ({comercio.Interacciones.Count} en total, "
                    + "de la mas reciente a la mas antigua)");

        foreach (var i in interacciones)
        {
            var fecha = i.Fecha.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var dias = (int)(hoy - i.Fecha).TotalDays;
            sb.AppendLine($"- {fecha} (hace {dias} dia(s)) · {i.Tipo} · {i.Detalle.Trim()}");
        }

        return sb.ToString();
    }
}
