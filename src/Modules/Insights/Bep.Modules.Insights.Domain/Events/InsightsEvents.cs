using Bep.SharedKernel;

namespace Bep.Modules.Insights.Domain.Events;

/// <summary>Se emite al generar un borrador de análisis de IA (RF-06-001). Audita el modelo usado.</summary>
public sealed record AnalisisGenerado(Guid AnalisisId, Guid EmpresaId, Guid CampanaId, string Modelo) : IDomainEvent
{
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

/// <summary>Se emite cuando un profesional valida el análisis: recién entonces es visible (RF-06-007).</summary>
public sealed record AnalisisValidado(Guid AnalisisId, Guid EmpresaId, Guid CampanaId, string ValidadoPorSubjectId) : IDomainEvent
{
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

/// <summary>Se emite al descartar un borrador de análisis.</summary>
public sealed record AnalisisDescartado(Guid AnalisisId, Guid EmpresaId, string Motivo) : IDomainEvent
{
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}
