using System.Text.Json;
using Bep.Application.Abstractions;
using Bep.Infrastructure.Common.Persistence;
using Bep.Modules.Audit.Domain;
using Bep.Modules.Audit.Infrastructure.Persistence;
using MediatR;

namespace Bep.Modules.Audit.Infrastructure.DomainEvents;

/// <summary>
/// Persiste cada evento de dominio como registro de auditoría inmutable (M8,
/// patrón Observer). Captura el tenant y el actor del contexto de la operación.
///
/// <para>
/// Nota de consistencia: en este incremento la escritura de auditoría ocurre tras
/// la confirmación del cambio de negocio (despacho in-process). La garantía
/// transaccional estricta (negocio + auditoría atómicos) requiere un patrón
/// Outbox, contemplado como evolución (ver dossier de arquitectura, KISS).
/// </para>
/// </summary>
internal sealed class PersistAuditLogHandler(
    AuditDbContext auditDbContext,
    ITenantContext tenantContext,
    ICurrentUser currentUser)
    : INotificationHandler<DomainEventNotification>
{
    public async Task Handle(DomainEventNotification notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;
        var payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType());

        var log = AuditLog.Record(
            eventType: domainEvent.GetType().Name,
            occurredOnUtc: domainEvent.OccurredOnUtc,
            tenantId: tenantContext.TenantId,
            actorSubjectId: currentUser.SubjectId,
            payloadJson: payload);

        auditDbContext.AuditLogs.Add(log);
        await auditDbContext.SaveChangesAsync(cancellationToken);
    }
}
