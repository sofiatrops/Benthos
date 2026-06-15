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

    /// <summary>Gestión y revisión de informes (revisor, coordinador, super-admin).</summary>
    public const string GestionarInformes = "reporting:gestionar-informes";

    /// <summary>Archivado (eliminación lógica) de informes, restringido a roles administrativos (RF-05-010).</summary>
    public const string ArchivarInformes = "reporting:archivar-informes";

    /// <summary>Acceso al Portal Cliente, exclusivo de usuarios de empresa cliente (RF-07-001).</summary>
    public const string PortalCliente = "portal:cliente";

    /// <summary>Emisión de tickets de subida de archivos (personal Benthos con rol de contenido, ADR-008).</summary>
    public const string SubirArchivos = "storage:subir-archivos";

    /// <summary>Ingesta y validación de resultados de laboratorio (revisor, coordinador, super-admin) (RF-04).</summary>
    public const string GestionarResultados = "laboratory:gestionar-resultados";

    /// <summary>Generación y validación de análisis de IA ambiental (revisor, coordinador, super-admin) (RF-06).</summary>
    public const string GestionarAnalisis = "insights:gestionar-analisis";
}
