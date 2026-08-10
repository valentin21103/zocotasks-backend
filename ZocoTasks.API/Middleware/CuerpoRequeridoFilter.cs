using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ZocoTasks.API.Middleware;

/// <summary>
/// Corta con 400 las peticiones cuyo cuerpo no se pudo deserializar.
/// </summary>
/// <remarks>
/// Hace falta por una consecuencia no obvia de <c>SuppressModelStateInvalidFilter</c>.
/// Esa opcion esta activada a proposito, para que la validacion la haga
/// FluentValidation desde los servicios y no compitan dos formatos de error.
/// El efecto colateral es que, cuando el JSON viene roto o el cuerpo viene
/// vacio, MVC ya no responde solo: pasa <c>null</c> como parametro y el servicio
/// explota mas adelante con un 500 generico.
///
/// Un cuerpo mal formado es un error del cliente, no una falla del servidor.
/// Este filtro lo devuelve como 400 con el mismo formato ProblemDetails que el
/// resto de la API, antes de que llegue a la capa de negocio.
/// </remarks>
public sealed class CuerpoRequeridoFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        foreach (var parametro in context.ActionDescriptor.Parameters)
        {
            if (parametro.BindingInfo?.BindingSource != BindingSource.Body)
            {
                continue;
            }

            var llego = context.ActionArguments.TryGetValue(parametro.Name, out var valor);
            if (llego && valor is not null)
            {
                continue;
            }

            var problema = new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "El cuerpo de la peticion es obligatorio.",
                Detail = "No se pudo leer el JSON enviado. Revisa el formato y el Content-Type.",
                Instance = context.HttpContext.Request.Path
            };

            problema.Extensions["codigo"] = "cuerpo_invalido";

            context.Result = new ObjectResult(problema)
            {
                StatusCode = StatusCodes.Status400BadRequest,
                ContentTypes = { "application/problem+json" }
            };

            return;
        }
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
