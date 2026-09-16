using System.Globalization;
using System.Text;

namespace Contratos.Infrastructure.Persistence;

/// <summary>
/// Genera un PDF minimo y valido para los contratos de demostracion.
///
/// Se construye a mano para no anadir una libreria de PDF solo para esto. Un PDF
/// exige una tabla de referencias (xref) con la posicion exacta en bytes de cada
/// objeto; si no coincide, los visores lo muestran danado. Por eso las
/// posiciones se calculan mientras se escribe, en lugar de fijarlas a mano.
/// </summary>
internal static class DocumentoDemo
{
    public static byte[] Generar(string titulo, string detalle)
    {
        var contenido =
            "BT /F1 20 Tf 72 740 Td (" + Escapar(titulo) + ") Tj ET\n" +
            "BT /F1 12 Tf 72 712 Td (" + Escapar(detalle) + ") Tj ET\n" +
            "BT /F1 10 Tf 72 690 Td (Documento de demostracion generado automaticamente.) Tj ET";

        var objetos = new[]
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] " +
            "/Contents 4 0 R /Resources << /Font << /F1 5 0 R >> >> >>",
            $"<< /Length {Encoding.ASCII.GetByteCount(contenido)} >>\nstream\n{contenido}\nendstream",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
        };

        using var salida = new MemoryStream();
        var posiciones = new List<long>();

        Escribir(salida, "%PDF-1.4\n");

        for (var i = 0; i < objetos.Length; i++)
        {
            posiciones.Add(salida.Position);
            Escribir(salida, $"{i + 1} 0 obj\n{objetos[i]}\nendobj\n");
        }

        var inicioXref = salida.Position;
        var xref = new StringBuilder();
        xref.Append(CultureInfo.InvariantCulture, $"xref\n0 {objetos.Length + 1}\n");
        xref.Append("0000000000 65535 f \n");
        foreach (var posicion in posiciones)
        {
            xref.Append(CultureInfo.InvariantCulture, $"{posicion:D10} 00000 n \n");
        }

        xref.Append(CultureInfo.InvariantCulture,
            $"trailer\n<< /Size {objetos.Length + 1} /Root 1 0 R >>\nstartxref\n{inicioXref}\n%%EOF\n");
        Escribir(salida, xref.ToString());

        return salida.ToArray();
    }

    private static void Escribir(Stream destino, string texto)
    {
        var bytes = Encoding.ASCII.GetBytes(texto);
        destino.Write(bytes, 0, bytes.Length);
    }

    /// <summary>
    /// Los parentesis y la barra invertida delimitan cadenas en PDF. Los
    /// caracteres fuera de ASCII se sustituyen: la fuente base no los representa.
    /// </summary>
    private static string Escapar(string texto)
    {
        var normalizado = texto.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();

        foreach (var c in normalizado)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (c is '(' or ')' or '\\')
            {
                sb.Append('\\');
            }

            sb.Append(c < 128 ? c : '?');
        }

        return sb.ToString();
    }
}
