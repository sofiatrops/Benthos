namespace Bep.Application.Abstractions;

/// <summary>
/// Contexto de tenant <b>efectivo</b> de la petición. Resuelto una sola vez por
/// el middleware de tenant y consumido por la capa de persistencia para fijar
/// <c>app.current_tenant</c> en PostgreSQL (RLS, ADR-004).
///
/// <para>
/// Para un usuario cliente, el tenant efectivo es inmutable (su propia empresa).
/// Para personal de Benthos con acceso transversal, el tenant objetivo se fija
/// de forma <b>explícita</b> según el recurso de la operación; nunca hay un
/// bypass global silencioso.
/// </para>
/// </summary>
public interface ITenantContext
{
    /// <summary>Tenant efectivo, o null si aún no se ha resuelto (p. ej. endpoints anónimos).</summary>
    public Guid? TenantId { get; }

    public bool HasTenant { get; }

    /// <summary>
    /// Fija el tenant efectivo de la petición. Idempotente por diseño: solo debe
    /// invocarse una vez por el middleware de tenant.
    /// </summary>
    public void SetTenant(Guid tenantId);
}
