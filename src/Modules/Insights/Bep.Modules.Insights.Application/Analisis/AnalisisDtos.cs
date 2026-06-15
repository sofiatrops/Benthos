namespace Bep.Modules.Insights.Application.Analisis;

public sealed record HallazgoDto(string Parametro, string Severidad, string Detalle);

/// <summary>Detalle de un análisis ambiental.</summary>
public sealed record AnalisisDto(
    Guid Id,
    Guid CampanaId,
    string Estado,
    string Resumen,
    string Modelo,
    DateTimeOffset GeneradoUtc,
    DateTimeOffset? ValidadoUtc,
    string? ValidadoPorSubjectId,
    string? MotivoDescarte,
    IReadOnlyList<HallazgoDto> Hallazgos);

/// <summary>Fila resumida de análisis para listados.</summary>
public sealed record AnalisisResumenDto(
    Guid Id, Guid CampanaId, string Estado, string Modelo, DateTimeOffset GeneradoUtc, int CantidadHallazgos);
