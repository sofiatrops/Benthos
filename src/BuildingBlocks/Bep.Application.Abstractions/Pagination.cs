namespace Bep.Application.Abstractions;

/// <summary>
/// Parámetros de paginación. Todo listado con potencial de superar 50 registros
/// debe paginarse en el backend (RNF-REND-005).
/// </summary>
public sealed record PageRequest(int Page = 1, int PageSize = 20)
{
    public const int MaxPageSize = 100;

    /// <summary>Página normalizada (mínimo 1).</summary>
    public int NormalizedPage => Page < 1 ? 1 : Page;

    /// <summary>Tamaño de página normalizado (entre 1 y <see cref="MaxPageSize"/>).</summary>
    public int NormalizedPageSize => Math.Clamp(PageSize, 1, MaxPageSize);

    public int Skip => (NormalizedPage - 1) * NormalizedPageSize;
}

/// <summary>Resultado paginado de una consulta.</summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, long TotalCount)
{
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
