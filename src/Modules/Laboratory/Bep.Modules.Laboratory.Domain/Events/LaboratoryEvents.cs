using Bep.SharedKernel;

namespace Bep.Modules.Laboratory.Domain.Events;

/// <summary>Se emite al ingestar un lote de resultados de laboratorio (RF-04-001). Audita.</summary>
public sealed record LoteResultadosRecibido(Guid LoteId, Guid EmpresaId, Guid CampanaId, int CantidadResultados) : IDomainEvent
{
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

/// <summary>Se emite al validar un lote: sus parámetros pasan a alimentar los indicadores (RF-04-005).</summary>
public sealed record LoteResultadosValidado(Guid LoteId, Guid EmpresaId, Guid CampanaId, int CantidadResultados) : IDomainEvent
{
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

/// <summary>Se emite al rechazar un lote (datos inconsistentes); no alimenta indicadores.</summary>
public sealed record LoteResultadosRechazado(Guid LoteId, Guid EmpresaId, string Motivo) : IDomainEvent
{
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}
