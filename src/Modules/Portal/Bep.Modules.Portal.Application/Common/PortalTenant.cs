using Bep.Application.Abstractions;

namespace Bep.Modules.Portal.Application.Common;

/// <summary>
/// Resuelve el tenant del Portal <b>exclusivamente</b> desde la identidad del
/// usuario cliente autenticado (claim del JWT), nunca desde la URL ni parámetros
/// de la API. Es la garantía estructural de RF-07-010: un usuario cliente no puede
/// acceder a datos de otra empresa manipulando la petición.
/// </summary>
internal static class PortalTenant
{
    public static Result<Guid> Resolver(ICurrentUser currentUser, ITenantContext tenantContext)
    {
        if (currentUser.PrincipalType != PrincipalType.ClientUser || currentUser.TenantId is not { } tenantId)
        {
            return Result.Failure<Guid>(Error.Forbidden(
                "portal.acceso_denegado",
                "El Portal Cliente solo es accesible para usuarios de una empresa cliente."));
        }

        // Fija el tenant efectivo (idempotente: el middleware ya lo fijó al mismo valor).
        tenantContext.SetTenant(tenantId);
        return Result.Success(tenantId);
    }
}
