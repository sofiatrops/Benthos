namespace Bep.Application.Abstractions.Security;

/// <summary>
/// Roles RBAC de la plataforma (Apéndice C del SRS). Los nombres coinciden con
/// los roles emitidos por Keycloak en el claim de roles del JWT.
/// </summary>
public static class BepRoles
{
    // Personal de Benthos (acceso transversal a tenants).
    public const string SuperAdministrador = "super-admin";
    public const string CoordinadorOperaciones = "coordinador";
    public const string TecnicoTerreno = "tecnico";
    public const string RevisorTecnico = "revisor";

    // Usuarios de empresa cliente (acotados a su propio tenant).
    public const string AdminEmpresaCliente = "admin-empresa";
    public const string UsuarioCliente = "usuario-cliente";
}

/// <summary>Nombres de políticas de autorización aplicadas en los endpoints.</summary>
public static class BepPolicies
{
    /// <summary>Solo personal de Benthos con rol de administración global.</summary>
    public const string GestionarEmpresas = "organization:gestionar-empresas";

    /// <summary>Personal de Benthos que planifica/opera (coordinador o super-admin).</summary>
    public const string GestionarCentros = "organization:gestionar-centros";

    /// <summary>Planificación y gestión de campañas (coordinador o super-admin).</summary>
    public const string GestionarCampanas = "campaign:gestionar-campanas";

    /// <summary>Registro y manejo de muestras en terreno y laboratorio (técnico, coordinador, super-admin).</summary>
    public const string GestionarMuestras = "sampling:gestionar-muestras";
}
