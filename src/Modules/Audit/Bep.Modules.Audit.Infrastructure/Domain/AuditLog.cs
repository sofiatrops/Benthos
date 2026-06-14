namespace Bep.Modules.Audit.Domain;

/// <summary>
/// Registro inmutable de auditoría (M8). Se crea una vez y nunca se modifica ni
/// elimina desde la aplicación (RF-08-007); la inmutabilidad se refuerza además
/// con un trigger en la base de datos.
/// </summary>
public sealed class AuditLog
{
    private AuditLog(
        Guid id,
        DateTimeOffset occurredOnUtc,
        string eventType,
        Guid? tenantId,
        string? actorSubjectId,
        string payloadJson)
    {
        Id = id;
        OccurredOnUtc = occurredOnUtc;
        EventType = eventType;
        TenantId = tenantId;
        ActorSubjectId = actorSubjectId;
        PayloadJson = payloadJson;
    }

    // Constructor para EF Core.
    private AuditLog() { }

    public Guid Id { get; private set; }

    /// <summary>Instante del evento auditado (UTC).</summary>
    public DateTimeOffset OccurredOnUtc { get; private set; }

    /// <summary>Tipo de evento de dominio (p. ej. <c>EmpresaRegistrada</c>).</summary>
    public string EventType { get; private set; } = null!;

    /// <summary>Empresa (tenant) afectada, si aplica. Permite filtrar la auditoría (RF-08-006).</summary>
    public Guid? TenantId { get; private set; }

    /// <summary>Sujeto que originó la acción (claim <c>sub</c>), o nulo si fue el sistema.</summary>
    public string? ActorSubjectId { get; private set; }

    /// <summary>Carga del evento serializada (JSON), con los datos relevantes.</summary>
    public string PayloadJson { get; private set; } = null!;

    public static AuditLog Record(
        string eventType,
        DateTimeOffset occurredOnUtc,
        Guid? tenantId,
        string? actorSubjectId,
        string payloadJson)
        => new(Guid.NewGuid(), occurredOnUtc, eventType, tenantId, actorSubjectId, payloadJson);
}
