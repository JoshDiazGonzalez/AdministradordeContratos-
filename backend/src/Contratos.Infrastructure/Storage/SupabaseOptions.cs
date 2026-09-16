namespace Contratos.Infrastructure.Storage;

public class SupabaseOptions
{
    public const string SectionName = "Supabase";

    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Clave de servicio. Solo vive en el backend: expuesta en el navegador daria
    /// acceso total al proyecto, saltandose RLS.
    /// </summary>
    public string ServiceRoleKey { get; set; } = string.Empty;

    public string StorageBucket { get; set; } = "contracts";
}
