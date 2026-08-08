using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ZocoTasks.Domain.Exceptions;

namespace ZocoTasks.API.Middleware;

/// <summary>
/// Traduce excepciones a respuestas HTTP.
/// </summary>
/// <remarks>
/// Centralizarlo evita que cada controller repita bloques try/catch y garantiza
/// que ninguna excepcion se escape como un 500 sin formato.
///
/// Todas las respuestas usan <c>ProblemDetails</c> (RFC 7807), que es el
/// formato estandar de errores HTTP: un cliente generico sabe leerlo sin
/// documentacion.
/// </remarks>
public class GlobalExceptionHandler : IMiddleware
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment _entorno;

    public GlobalExceptionHandler(
        ILogger<GlobalExceptionHandler> logger, IHostEnvironment entorno)
    {
        _logger = logger;
        _entorno = entorno;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await Responder(context, ex);
        }
    }

    private async Task Responder(HttpContext context, Exception ex)
    {
        // Si ya se empezo a escribir la respuesta no se pueden cambiar los
        // headers; lo unico util es dejar constancia en el log.
        if (context.Response.HasStarted)
        {
            _logger.LogError(ex, "Excepcion despues de empezar a escribir la respuesta en {Ruta}",
                context.Request.Path);
            return;
        }

        var problema = Traducir(context, ex);

        context.Response.StatusCode = problema.Status ?? StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";

        // El generico va como <object> a proposito: si se serializa con el tipo
        // estatico ProblemDetails, System.Text.Json descarta las propiedades de
        // las clases derivadas y el diccionario de errores por campo de
        // ValidationProblemDetails nunca llega al cliente.
        await context.Response.WriteAsJsonAsync<object>(problema);
    }

    private ProblemDetails Traducir(HttpContext context, Exception ex)
    {
        switch (ex)
        {
            // 400: el cuerpo de la peticion no cumple las reglas de formato.
            // Se devuelve el detalle campo por campo para que el front pueda
            // marcarlos en el formulario.
            case ValidationException validacion:
                _logger.LogInformation("Validacion fallida en {Ruta}", context.Request.Path);

                var errores = validacion.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

                return new ValidationProblemDetails(errores)
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Hay datos invalidos.",
                    Instance = context.Request.Path
                };

            // 428: falta el header If-Match en una operacion que modifica.
            // Existe justamente para esto: el servidor exige que la peticion sea
            // condicional, para que no se pueda pisar el trabajo de otro sin
            // siquiera enterarse.
            case PrecondicionRequeridaException precondicion:
                _logger.LogInformation("Falta precondicion en {Ruta}", context.Request.Path);

                return Armar(context, StatusCodes.Status428PreconditionRequired,
                    "Falta el header If-Match", precondicion.Message, precondicion.Codigo);

            // 404
            case EntidadNoEncontradaException noEncontrada:
                _logger.LogInformation("No encontrado: {Mensaje}", noEncontrada.Message);

                return Armar(context, StatusCodes.Status404NotFound,
                    "Recurso no encontrado", noEncontrada.Message, noEncontrada.Codigo);

            // 409: la transicion de estado pedida no existe en el pipeline.
            // Es un conflicto con el estado actual del recurso, igual que el
            // conflicto de concurrencia.
            case EstadoTransicionInvalidaException transicion:
                _logger.LogInformation("Transicion invalida: {Mensaje}", transicion.Message);

                var problemaTransicion = Armar(context, StatusCodes.Status409Conflict,
                    "Transicion de estado invalida", transicion.Message, transicion.Codigo);

                problemaTransicion.Extensions["estadoActual"] = transicion.Desde.ToString();
                problemaTransicion.Extensions["estadoSolicitado"] = transicion.Hacia.ToString();

                return problemaTransicion;

            // 409: dos usuarios editaron el mismo registro. Este es el
            // requisito destacado de la consigna.
            case DbUpdateConcurrencyException:
                _logger.LogWarning("Conflicto de concurrencia en {Ruta}", context.Request.Path);

                var conflicto = Armar(context, StatusCodes.Status409Conflict,
                    "El registro fue modificado por otro usuario",
                    "Otro usuario modifico este comercio mientras lo estabas editando. " +
                    "Volve a cargarlo para ver los cambios y aplica los tuyos de nuevo.",
                    "conflicto_de_concurrencia");

                // Le dice al cliente como resolverlo sin tener que adivinar.
                conflicto.Extensions["comoResolver"] =
                    "Hace un GET del recurso, tomá el ETag nuevo y reintentá el PUT con ese valor.";

                return conflicto;

            // 422: la peticion esta bien formada, pero una regla de negocio
            // impide procesarla (CUIT repetido, rubro inexistente).
            case ReglaDeNegocioException regla:
                _logger.LogInformation("Regla de negocio: {Mensaje}", regla.Message);

                return Armar(context, StatusCodes.Status422UnprocessableEntity,
                    "No se pudo completar la operacion", regla.Message, regla.Codigo);

            // 400: violacion de una restriccion de la base que no se atajo
            // antes. No se expone el mensaje de Postgres, que puede filtrar
            // nombres de tablas y columnas.
            case DbUpdateException baseDeDatos:
                _logger.LogError(baseDeDatos, "Error al guardar en {Ruta}", context.Request.Path);

                return Armar(context, StatusCodes.Status400BadRequest,
                    "No se pudo guardar",
                    "Los datos enviados violan una restriccion de la base de datos.",
                    "error_de_persistencia");

            // 499: el cliente corto la conexion. No es un error del servidor.
            case OperationCanceledException when context.RequestAborted.IsCancellationRequested:
                _logger.LogInformation("Peticion cancelada por el cliente en {Ruta}",
                    context.Request.Path);

                return Armar(context, 499, "Peticion cancelada",
                    "El cliente cancelo la peticion.", "cancelada");

            // 500: cualquier otra cosa. El detalle real va al log, no a la
            // respuesta: filtrar stack traces es una fuga de informacion.
            default:
                _logger.LogError(ex, "Error no controlado en {Ruta}", context.Request.Path);

                var interno = Armar(context, StatusCodes.Status500InternalServerError,
                    "Error interno del servidor",
                    "Ocurrio un error inesperado. Si persiste, contactá al administrador.",
                    "error_interno");

                // Solo en desarrollo se agrega el detalle, para poder depurar.
                if (_entorno.IsDevelopment())
                {
                    interno.Extensions["excepcion"] = ex.ToString();
                }

                return interno;
        }
    }

    private static ProblemDetails Armar(
        HttpContext context, int estado, string titulo, string detalle, string codigo)
    {
        var problema = new ProblemDetails
        {
            Status = estado,
            Title = titulo,
            Detail = detalle,
            Instance = context.Request.Path
        };

        // Codigo estable para que el front discrimine el error sin depender del
        // texto, que puede cambiar o traducirse.
        problema.Extensions["codigo"] = codigo;

        return problema;
    }
}
