using System.Globalization;
using System.Text;

namespace Contratos.Domain.Services;

/// <summary>
/// Normaliza texto para busquedas: minusculas, sin tildes ni dieresis y con los
/// espacios compactados.
///
/// Existe porque los usuarios buscan "pacifico" o "tecnologicos" sin tildes y
/// esperan encontrar "Seguridad Integral del Pacífico". La comparacion directa
/// en SQL distingue acentos, y la extension unaccent de PostgreSQL no existe en
/// SQLite (base local y tests). Guardar una version normalizada del nombre es
/// portable entre motores y puede indexarse.
/// </summary>
public static class TextoBusqueda
{
    public static string Normalizar(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return string.Empty;
        }

        // FormD separa cada letra de su acento ("á" -> "a" + acento combinante);
        // al descartar las marcas combinantes queda la letra base. La "ñ" pasa a
        // "n", igual que hace un teclado sin la tecla correspondiente.
        var descompuesto = texto.Normalize(NormalizationForm.FormD);
        var resultado = new StringBuilder(descompuesto.Length);
        var espacioPrevio = false;

        foreach (var caracter in descompuesto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(caracter) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsWhiteSpace(caracter))
            {
                if (!espacioPrevio && resultado.Length > 0)
                {
                    resultado.Append(' ');
                }

                espacioPrevio = true;
                continue;
            }

            resultado.Append(char.ToLowerInvariant(caracter));
            espacioPrevio = false;
        }

        return resultado.ToString().TrimEnd();
    }
}
