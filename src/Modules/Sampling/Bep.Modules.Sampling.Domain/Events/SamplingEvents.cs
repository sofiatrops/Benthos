using Bep.SharedKernel;

namespace Bep.Modules.Sampling.Domain.Events;

/// <summary>Se emite al registrar una muestra en terreno (RF-03-001). Audita y dispara trazabilidad.</summary>
public sealed record MuestraRegistrada(Guid MuestraId, Guid EmpresaId, Guid CampanaId, Guid CentroId, string CodigoUnico) : IDomainEvent
{
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

/// <summary>Se emite al transferir la custodia de una muestra (RF-03-007).</summary>
public sealed record CustodiaTransferida(Guid MuestraId, Guid EmpresaId, string? De, string Para) : IDomainEvent
{
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

/// <summary>Se emite al aceptar la custodia de una muestra (RF-03-007).</summary>
public sealed record CustodiaAceptada(Guid MuestraId, Guid EmpresaId, string Por) : IDomainEvent
{
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

/// <summary>Se emite en cada transición de estado de la muestra (RF-08-005).</summary>
public sealed record EstadoMuestraCambiado(Guid MuestraId, Guid EmpresaId, EstadoMuestra Anterior, EstadoMuestra Nuevo) : IDomainEvent
{
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}
