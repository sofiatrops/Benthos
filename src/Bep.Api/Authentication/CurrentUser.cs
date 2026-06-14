using System.Security.Claims;
using Bep.Application.Abstractions;

namespace Bep.Api.Authentication;

/// <summary>
/// Implementa <see cref="ICurrentUser"/> a partir del JWT validado de Keycloak.
/// Mapea el sujeto, el tipo de principal y el tenant de pertenencia.
/// </summary>
public sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public string? SubjectId => Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? Principal?.FindFirstValue("sub");

    public Guid? TenantId =>
        Guid.TryParse(Principal?.FindFirstValue(BepClaimTypes.TenantId), out var tenantId)
            ? tenantId
            : null;

    public PrincipalType PrincipalType
    {
        get
        {
            if (!IsAuthenticated)
            {
                return PrincipalType.Anonymous;
            }

            // Claim explícito emitido por Keycloak; si falta, se infiere por la
            // presencia de tenant (usuario cliente) vs. ausencia (personal Benthos).
            return Principal?.FindFirstValue(BepClaimTypes.PrincipalType) switch
            {
                "benthos" => PrincipalType.BenthosStaff,
                "client" => PrincipalType.ClientUser,
                _ => TenantId.HasValue ? PrincipalType.ClientUser : PrincipalType.BenthosStaff,
            };
        }
    }

    // Alineado con TokenValidationParameters.RoleClaimType = "roles" (Keycloak).
    public IReadOnlyCollection<string> Roles =>
        Principal?.FindAll("roles").Select(c => c.Value).ToArray() ?? [];
}

/// <summary>Nombres de claims propios de BEP emitidos por Keycloak.</summary>
public static class BepClaimTypes
{
    public const string TenantId = "tenant_id";
    public const string PrincipalType = "principal_type";
}
