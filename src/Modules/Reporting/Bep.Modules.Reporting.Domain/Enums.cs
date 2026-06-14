namespace Bep.Modules.Reporting.Domain;

/// <summary>Tipo de estudio que reporta el informe (RF-05-006).</summary>
public enum TipoEstudio
{
    CaracterizacionAmbiental = 1,
    CalidadAgua = 2,
    Macroinvertebrados = 3,
    Microinvertebrados = 4,
    Mixto = 5,
}

/// <summary>
/// Estados del flujo de revisión y publicación de un informe (RF-05-003).
/// Solo los informes en <see cref="Publicado"/> son visibles para el cliente
/// (RF-05-005). El estado <see cref="Archivado"/> es la única forma de "eliminar"
/// un informe publicado (eliminación lógica, RF-05-010).
/// </summary>
public enum EstadoInforme
{
    Borrador = 1,
    EnRevision = 2,
    CambiosSolicitados = 3,
    Aprobado = 4,
    Publicado = 5,
    Archivado = 6,
}
