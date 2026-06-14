using Bep.SharedKernel;

namespace Bep.Modules.Reporting.Domain.Events;

/// <summary>Se emite al crear un informe (RF-05-001). Audita.</summary>
public sealed record InformeCreado(Guid InformeId, Guid EmpresaId, string Titulo) : IDomainEvent
{
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

/// <summary>Se emite al cargar una nueva versión de informe (RF-05-002).</summary>
public sealed record VersionInformeCargada(Guid InformeId, Guid EmpresaId, int Numero) : IDomainEvent
{
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

/// <summary>Se emite en cada transición del flujo de revisión (RF-08-005).</summary>
public sealed record EstadoInformeCambiado(Guid InformeId, Guid EmpresaId, EstadoInforme Anterior, EstadoInforme Nuevo) : IDomainEvent
{
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

/// <summary>Se emite al publicar un informe. Dispara la notificación al cliente (RF-05-008).</summary>
public sealed record InformePublicado(Guid InformeId, Guid EmpresaId) : IDomainEvent
{
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

/// <summary>Se emite al archivar (eliminación lógica) un informe (RF-05-010).</summary>
public sealed record InformeArchivado(Guid InformeId, Guid EmpresaId) : IDomainEvent
{
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}
