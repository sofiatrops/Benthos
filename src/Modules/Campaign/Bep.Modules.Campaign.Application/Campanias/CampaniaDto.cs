namespace Bep.Modules.Campaign.Application.Campanias;

public sealed record CampaniaDto(
    Guid Id,
    Guid EmpresaId,
    string Nombre,
    string Descripcion,
    string Tipo,
    string Estado,
    DateOnly FechaInicio,
    DateOnly FechaFin,
    IReadOnlyList<Guid> CentroIds,
    IReadOnlyList<ResponsableDto> Responsables);

public sealed record ResponsableDto(string SubjectId, string Rol);
