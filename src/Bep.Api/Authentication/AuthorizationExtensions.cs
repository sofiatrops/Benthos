using Bep.Application.Abstractions.Security;

namespace Bep.Api.Authentication;

public static class AuthorizationExtensions
{
    /// <summary>
    /// Define las políticas RBAC del SRS (Apéndice C). El detalle fino por acción
    /// se afina por módulo conforme a RF-01-006.
    /// </summary>
    public static IServiceCollection AddBepAuthorization(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            .AddPolicy(BepPolicies.GestionarEmpresas, policy =>
                policy.RequireRole(BepRoles.SuperAdministrador))
            .AddPolicy(BepPolicies.GestionarCentros, policy =>
                policy.RequireRole(BepRoles.SuperAdministrador, BepRoles.CoordinadorOperaciones))
            .AddPolicy(BepPolicies.GestionarCampanas, policy =>
                policy.RequireRole(BepRoles.SuperAdministrador, BepRoles.CoordinadorOperaciones))
            .AddPolicy(BepPolicies.GestionarMuestras, policy =>
                policy.RequireRole(BepRoles.SuperAdministrador, BepRoles.CoordinadorOperaciones, BepRoles.TecnicoTerreno))
            .AddPolicy(BepPolicies.GestionarInformes, policy =>
                policy.RequireRole(BepRoles.SuperAdministrador, BepRoles.CoordinadorOperaciones, BepRoles.RevisorTecnico))
            .AddPolicy(BepPolicies.ArchivarInformes, policy =>
                policy.RequireRole(BepRoles.SuperAdministrador))
            .AddPolicy(BepPolicies.PortalCliente, policy =>
                policy.RequireRole(BepRoles.AdminEmpresaCliente, BepRoles.UsuarioCliente))
            .AddPolicy(BepPolicies.SubirArchivos, policy =>
                policy.RequireRole(
                    BepRoles.SuperAdministrador, BepRoles.CoordinadorOperaciones,
                    BepRoles.RevisorTecnico, BepRoles.TecnicoTerreno));

        return services;
    }
}
