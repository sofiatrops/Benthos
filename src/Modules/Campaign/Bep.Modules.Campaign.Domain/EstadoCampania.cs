namespace Bep.Modules.Campaign.Domain;

/// <summary>Estados del ciclo de vida de una campaña (RF-02-003).</summary>
public enum EstadoCampania
{
    Planificada = 1,
    EnCurso = 2,
    EnRevision = 3,
    Cerrada = 4,
    Cancelada = 5,
}
