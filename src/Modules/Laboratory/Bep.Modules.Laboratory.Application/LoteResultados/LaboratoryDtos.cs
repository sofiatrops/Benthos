namespace Bep.Modules.Laboratory.Application.LoteResultados;

/// <summary>Medición individual reportada por el laboratorio.</summary>
public sealed record ResultadoParametroDto(
    string CodigoMuestra, string Parametro, double Valor, string Unidad, string? Metodo);

/// <summary>Detalle de un lote de resultados con sus mediciones.</summary>
public sealed record LoteResultadosDto(
    Guid Id,
    Guid CampanaId,
    string Laboratorio,
    string Estado,
    DateTimeOffset RecibidoUtc,
    DateTimeOffset? ValidadoUtc,
    string? MotivoRechazo,
    string ArchivoObjectKey,
    IReadOnlyList<ResultadoParametroDto> Resultados);

/// <summary>Fila resumida de lote para listados.</summary>
public sealed record LoteResumenDto(
    Guid Id,
    Guid CampanaId,
    string Laboratorio,
    string Estado,
    int CantidadResultados,
    DateTimeOffset RecibidoUtc);

/// <summary>Indicador clave ambiental derivado de los resultados de laboratorio (RF-07-005).</summary>
public sealed record LaboratorioKpi(string Nombre, double Valor, string Unidad);
