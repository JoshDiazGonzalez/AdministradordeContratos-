namespace Contratos.Application.Storage;

public class StorageOptions
{
    public const string SectionName = "Storage";

    /// <summary>"Local" o "Supabase".</summary>
    public string Provider { get; set; } = "Local";

    /// <summary>Carpeta base del proveedor local, relativa al directorio de la app.</summary>
    public string LocalPath { get; set; } = "storage";

    public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024;

    public string[] AllowedExtensions { get; set; } = [".pdf", ".doc", ".docx"];
}
