namespace Bep.Application.Abstractions;

/// <summary>
/// Identidad del usuario autenticado en la petición actual, derivada del JWT
/// emitido por Keycloak (ADR-003). La autorización de negocio (rol × ámbito) se
/// evalúa sobre estos datos.
/// </summary>
public interface ICurrentUser
{
    public bool IsAuthenticated { get; }

    /// <summary>Identificador estable del sujeto (claim <c>sub</c> de Keycloak).</summary>
    public string? SubjectId { get; }

    public PrincipalType PrincipalType { get; }

    /// <summary>Tenant de pertenencia para un <see cref="PrincipalType.ClientUser"/>; null para personal de Benthos.</summary>
    public Guid? TenantId { get; }

    public IReadOnlyCollection<string> Roles { get; }
}
