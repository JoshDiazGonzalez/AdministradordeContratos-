namespace Contratos.Api.Extensions;

/// <summary>
/// Carga un archivo .env como variables de entorno del proceso.
///
/// Existe porque .NET no lee .env de forma nativa: sin esto habria que exportar
/// cada variable a mano antes de "dotnet run", o configurarlas en el perfil de
/// depuracion del IDE. Con esto, clonar el repositorio, copiar .env.example a
/// .env y pulsar F5 funciona.
///
/// Solo se usa en desarrollo. En produccion (Docker, nube) las variables las
/// inyecta el orquestador y este cargador no interviene.
/// </summary>
public static class EnvFileLoader
{
    /// <summary>
    /// Busca un archivo .env desde el directorio indicado hacia arriba y carga
    /// sus valores.
    /// </summary>
    /// <remarks>
    /// No sobrescribe variables ya definidas en el entorno: lo que venga del
    /// sistema o del IDE tiene prioridad sobre el archivo.
    /// </remarks>
    /// <returns>La ruta del archivo cargado, o null si no se encontro ninguno.</returns>
    public static string? Cargar(string directorioInicial, int nivelesMaximos = 6)
    {
        var directorio = new DirectoryInfo(directorioInicial);

        for (var nivel = 0; nivel < nivelesMaximos && directorio is not null; nivel++)
        {
            var candidato = Path.Combine(directorio.FullName, ".env");

            if (File.Exists(candidato))
            {
                AplicarArchivo(candidato);
                return candidato;
            }

            directorio = directorio.Parent;
        }

        return null;
    }

    private static void AplicarArchivo(string ruta)
    {
        foreach (var linea in File.ReadLines(ruta))
        {
            var texto = linea.Trim();

            if (texto.Length == 0 || texto.StartsWith('#'))
            {
                continue;
            }

            var separador = texto.IndexOf('=');
            if (separador <= 0)
            {
                continue;
            }

            var clave = texto[..separador].Trim();
            var valor = texto[(separador + 1)..].Trim();

            // Se admiten valores entrecomillados; las comillas no forman parte
            // del valor. Es habitual entrecomillar cadenas con espacios.
            if (valor.Length >= 2
                && ((valor[0] == '"' && valor[^1] == '"')
                    || (valor[0] == '\'' && valor[^1] == '\'')))
            {
                valor = valor[1..^1];
            }

            // Lo que ya exista en el entorno manda: permite sobrescribir un valor
            // puntual sin editar el archivo.
            if (Environment.GetEnvironmentVariable(clave) is null)
            {
                Environment.SetEnvironmentVariable(clave, valor);
            }
        }
    }
}
