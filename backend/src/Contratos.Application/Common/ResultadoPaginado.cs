namespace Contratos.Application.Common;

/// <summary>Pagina de resultados con los metadatos necesarios para el paginador.</summary>
public record ResultadoPaginado<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalItems)
{
    public int TotalPages =>
        PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalItems / (double)PageSize);
}
