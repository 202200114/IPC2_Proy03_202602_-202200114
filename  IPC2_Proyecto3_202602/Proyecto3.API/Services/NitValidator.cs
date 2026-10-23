using System.Text.RegularExpressions;

namespace Proyecto3.API.Services;

/// <summary>
/// Valida la forma y el dígito verificador de un NIT guatemalteco.
/// Se acepta con o sin guion. El algoritmo usa los factores 2, 3, 4... desde la derecha.
/// </summary>
///
// esta clase por su parte es de tipo static, las estaticas son unicas en el flujo del programa

public static class NitValidator
{
    public static bool EsValido(string? nit)
    {
        if (string.IsNullOrWhiteSpace(nit)) return false;
        var limpio = Regex.Replace(nit.Trim().ToUpperInvariant(), @"[\s-]", string.Empty);
        if (limpio.Length < 2 || limpio.Length > 21) return false;

        var cuerpo = limpio[..^1];
        var verificador = limpio[^1];
        if (!cuerpo.All(char.IsDigit)) return false;
        if (!char.IsDigit(verificador) && verificador != 'K') return false;

        long suma = 0;
        var factor = 2;
        for (var i = cuerpo.Length - 1; i >= 0; i--)
        {
            suma += (cuerpo[i] - '0') * factor;
            factor++;
        }

        var calculado = (11 - (suma % 11)) % 11;
        var caracterCalculado = calculado == 10 ? 'K' : (char)('0' + calculado);
        return verificador == caracterCalculado;
    }

    public static string Normalizar(string? nit) =>
        Regex.Replace((nit ?? string.Empty).Trim().ToUpperInvariant(), @"[\s-]", string.Empty);
}
