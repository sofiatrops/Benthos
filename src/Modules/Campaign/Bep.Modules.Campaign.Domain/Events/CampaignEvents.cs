using Bep.SharedKernel;

namespace Bep.Modules.Campaign.Domain.Events;

/// <summary>Se emite al crear una campaña. Consumido por Auditoría (M8).</summary>
public sealed record CampanaCreada(Guid CampanaId, Guid EmpresaId, string Nombre) : IDomainEvent
{
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

/// <summary>Se emite en cada transición de estado de campaña (RF-08-005).</summary>
public sealed record EstadoCampanaCambiado(
    Guid CampanaId, Guid EmpresaId, EstadoCampania Anterior, EstadoCampania Nuevo) : IDomainEvent
{
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

/// <summary>Se emite al cerrar una campaña. Punto de integración con Informes (M5).</summary>
public sealed record CampanaCerrada(Guid CampanaId, Guid EmpresaId) : IDomainEvent
{
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}
