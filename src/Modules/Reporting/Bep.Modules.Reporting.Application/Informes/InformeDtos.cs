namespace Bep.Modules.Reporting.Application.Informes;

/// <summary>Detalle completo de un informe para personal de Benthos (incluye comentarios internos).</summary>
public sealed record InformeDto(
    Guid Id,
    Guid EmpresaId,
    string Titulo,
    string TipoEstudio,
    DateOnly PeriodoDesde,
    DateOnly PeriodoHasta,
    Guid? CampanaId,
    Guid? CentroId,
    string AutorSubjectId,
    string Estado,
    DateTimeOffset CreadoUtc,
    DateTimeOffset? FechaAprobacionUtc,
    int VersionVigenteNumero,
    IReadOnlyList<VersionInformeDto> Versiones,
    IReadOnlyList<ComentarioInternoDto> Comentarios,
    IReadOnlyList<AnexoDto> Anexos);

public sealed record VersionInformeDto(int Numero, string ObjectKey, DateTimeOffset FechaCargaUtc, string? CargadoPorSubjectId);

public sealed record ComentarioInternoDto(string AutorSubjectId, string Texto, DateTimeOffset FechaUtc);

public sealed record AnexoDto(string ObjectKey, string Descripcion, DateTimeOffset FechaUtc);

/// <summary>Fila resumida para listados (RF-05). No expone comentarios internos.</summary>
public sealed record InformeResumenDto(
    Guid Id,
    string Titulo,
    string TipoEstudio,
    string Estado,
    DateOnly PeriodoDesde,
    DateOnly PeriodoHasta,
    int VersionVigenteNumero,
    DateTimeOffset CreadoUtc);
