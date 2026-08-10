namespace ZocoTasks.Domain.Common;

/// <summary>
/// Validacion de CUIT por el algoritmo de modulo 11 que usa AFIP.
/// </summary>
/// <remarks>
/// Vive en Domain porque es una regla del negocio, no de la capa web: un CUIT
/// invalido no deberia poder entrar al sistema por ningun camino, ni por la API
/// ni por una importacion masiva. Ademas, al ser una funcion pura, se testea
/// sin levantar nada.
/// </remarks>
public static class Cuit
{
    private static readonly int[] Pesos = [5, 4, 3, 2, 7, 6, 5, 4, 3, 2];


    public static string Normalizar(string? cuit)
    {
        if (string.IsNullOrWhiteSpace(cuit))
        {
            return string.Empty;
        }

        return new string(cuit.Where(char.IsDigit).ToArray());
    }

    /// <summary>
    /// Valida el CUIT. Acepta con o sin guiones: normaliza primero.
    /// </summary>
    public static bool EsValido(string? cuit)
    {
        var limpio = Normalizar(cuit);

        if (limpio.Length != 11)
        {
            return false;
        }

        // Un CUIT de once ceros pasa el modulo 11 pero no existe.
        if (limpio.All(c => c == '0'))
        {
            return false;
        }

        var suma = 0;
        for (var i = 0; i < 10; i++)
        {
            suma += (limpio[i] - '0') * Pesos[i];
        }

        var verificador = 11 - (suma % 11);

        if (verificador == 11)
        {
            verificador = 0;
        }
        else if (verificador == 10)
        {
            verificador = 9;
        }

        return verificador == limpio[10] - '0';
    }
}
