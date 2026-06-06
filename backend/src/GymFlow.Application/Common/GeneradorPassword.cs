using System.Security.Cryptography;

namespace GymFlow.Application.Common;

/// <summary>
/// Genera contraseñas temporales aleatorias criptográficamente seguras.
/// Garantiza al menos una mayúscula, una minúscula, un dígito y un símbolo.
/// </summary>
public static class GeneradorPassword
{
    private const string Mayusculas = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string Minusculas = "abcdefghijkmnopqrstuvwxyz";
    private const string Digitos = "23456789";
    private const string Simbolos = "!@#$%&*?-_";
    private const string Todos = Mayusculas + Minusculas + Digitos + Simbolos;

    /// <summary>
    /// Genera una contraseña temporal de la longitud indicada (mínimo 12).
    /// </summary>
    public static string Generar(int longitud = 14)
    {
        if (longitud < 12)
            longitud = 12;

        var chars = new char[longitud];

        // Garantizar al menos un carácter de cada categoría.
        chars[0] = ElegirAleatorio(Mayusculas);
        chars[1] = ElegirAleatorio(Minusculas);
        chars[2] = ElegirAleatorio(Digitos);
        chars[3] = ElegirAleatorio(Simbolos);

        // Relleno aleatorio.
        for (var i = 4; i < longitud; i++)
            chars[i] = ElegirAleatorio(Todos);

        Barajar(chars);

        return new string(chars);
    }

    private static char ElegirAleatorio(string fuente)
    {
        var indice = RandomNumberGenerator.GetInt32(fuente.Length);
        return fuente[indice];
    }

    private static void Barajar(char[] chars)
    {
        // Fisher-Yates con RNG criptográfico.
        for (var i = chars.Length - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }
    }
}
