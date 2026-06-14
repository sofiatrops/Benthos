using Bep.Application.Abstractions;

namespace Bep.Infrastructure.Common.Persistence;

/// <summary>
/// Implementación scoped del contexto de tenant. El middleware de tenant la
/// rellena una vez por petición; el resto del grafo la consume por inyección.
/// </summary>
public sealed class AmbientTenantContext : ITenantContext
{
    public Guid? TenantId { get; private set; }

    public bool HasTenant => TenantId.HasValue;

    public void SetTenant(Guid tenantId)
    {
        if (HasTenant && TenantId != tenantId)
        {
            throw new InvalidOperationException(
                "El tenant efectivo ya fue fijado para esta petición y no puede cambiarse.");
        }

        TenantId = tenantId;
    }
}
