namespace Bep.SharedKernel;

/// <summary>
/// Evento de dominio. Se publica al persistir el agregado y es consumido por
/// módulos como Auditoría (M8) y Notificaciones, sin acoplarlos al flujo
/// transaccional (patrón Observer, SRS 2.7.4).
/// </summary>
public interface IDomainEvent
{
    /// <summary>Instante en que ocurrió el evento (UTC, ISO 8601 — SRS 3.2.4).</summary>
    public DateTimeOffset OccurredOnUtc { get; }
}
