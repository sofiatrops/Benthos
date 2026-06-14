namespace Bep.Modules.Sampling.Application.Muestras;

/// <summary>Detalle completo de una muestra, incluyendo trazabilidad y custodia.</summary>
public sealed record MuestraDto(
    Guid Id,
    Guid EmpresaId,
    Guid CampanaId,
    Guid CentroId,
    string CodigoUnico,
    string CodigoQr,
    string Tipo,
    string Estado,
    double Latitud,
    double Longitud,
    double? PrecisionMetros,
    DateTimeOffset FechaRegistroUtc,
    IReadOnlyList<string> Parametros,
    IReadOnlyList<string> Fotos,
    IReadOnlyList<EventoMuestraDto> Eventos,
    IReadOnlyList<CustodiaDto> Custodias);

public sealed record EventoMuestraDto(string Tipo, DateTimeOffset FechaUtc, string? UsuarioSubjectId, string Descripcion);

public sealed record CustodiaDto(
    string? De, string Para, DateTimeOffset FechaTransferenciaUtc, bool Aceptada, DateTimeOffset? FechaAceptacionUtc);

/// <summary>Fila resumida para el listado/exportación de muestras de una campaña (RF-03-012).</summary>
public sealed record MuestraResumenDto(
    Guid Id,
    string CodigoUnico,
    string CodigoQr,
    Guid CentroId,
    string Tipo,
    string Estado,
    DateTimeOffset FechaRegistroUtc);
