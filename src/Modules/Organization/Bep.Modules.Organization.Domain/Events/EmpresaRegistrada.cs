using Bep.SharedKernel;

namespace Bep.Modules.Organization.Domain.Events;

/// <summary>Se emite al registrar una nueva empresa (tenant). Consumido por Auditoría (M8).</summary>
public sealed record EmpresaRegistrada(Guid EmpresaId, string RazonSocial) : IDomainEvent
{
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}
