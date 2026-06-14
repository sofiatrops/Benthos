namespace Bep.Application.Abstractions;

/// <summary>
/// Tipo de sujeto autenticado. Distingue al personal de Benthos (acceso
/// transversal a tenants) de los usuarios de una empresa cliente (acotados a su
/// propio tenant). Es el eje del modelo de autorización Rol × Ámbito × Tenant
/// (doc 02-dominio-y-contextos, §4).
/// </summary>
public enum PrincipalType
{
    Anonymous = 0,
    BenthosStaff = 1,
    ClientUser = 2,
}
