using ZocoTasks.Domain.Exceptions;

namespace ZocoTasks.API;

/// <summary>
/// Traduccion entre el token de concurrencia (<c>xmin</c>, un <see cref="uint"/>)
/// y los headers HTTP <c>ETag</c> / <c>If-Match</c>.
/// </summary>
/// <remarks>
/// Se usan headers estandar (RFC 9110) en lugar de un campo en el cuerpo porque
/// cualquier cliente, proxy o herramienta de API los entiende sin necesidad de
/// documentacion propia.
/// </remarks>
public static class ETagHelper
{
    /// <summary>Formato de ETag: el numero entre comillas dobles.</summary>
    public static string Formatear(uint version) => $"\"{version}\"";

    public static void EscribirEnRespuesta(HttpResponse response, uint version)
    {
        response.Headers.ETag = Formatear(version);
    }

    /// <summary>
    /// Lee <c>If-Match</c> de la peticion. Es obligatorio en toda operacion que
    /// modifica: sin el no hay forma de detectar que otro usuario escribio en el
    /// medio, que es justamente lo que hay que evitar.
    /// </summary>
    /// <exception cref="PrecondicionRequeridaException">Si el header no vino.</exception>
    /// <exception cref="ReglaDeNegocioException">Si el header vino con un valor ilegible.</exception>
    public static uint LeerIfMatch(HttpRequest request)
    {
        var valor = request.Headers.IfMatch.ToString();

        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new PrecondicionRequeridaException(
                "Falta el header If-Match. Tomá el valor del ETag que devolvió el GET " +
                "y enviálo en If-Match para que se pueda detectar si otro usuario " +
                "modificó el registro mientras lo editabas.");
        }

        // Se admite con o sin comillas, y se ignora el prefijo W/ de los ETag
        // debiles por si algun proxy lo agrega.
        var limpio = valor.Trim().TrimStart('W', '/').Trim('"');

        if (!uint.TryParse(limpio, out var version))
        {
            throw new ReglaDeNegocioException(
                $"El header If-Match tiene un valor invalido: '{valor}'. " +
                "Debe ser el ETag que devolvio el GET del recurso.");
        }

        return version;
    }
}
