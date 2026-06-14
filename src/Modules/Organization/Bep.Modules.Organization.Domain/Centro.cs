using Bep.SharedKernel;

namespace Bep.Modules.Organization.Domain;

/// <summary>
/// Centro operativo de una empresa (p. ej. un centro de cultivo de salmón),
/// sobre el cual se ejecutan campañas de monitoreo (RF-01-002). Es
/// <see cref="ITenantScoped"/>: queda sujeto al aislamiento por tenant en capa
/// de aplicación y a Row-Level Security en PostgreSQL (ADR-004).
/// </summary>
public sealed class Centro : Entity<Guid>, ITenantScoped
{
    private Centro(Guid id, Guid tenantId, string nombre, string codigoInterno, CoordenadasGps coordenadas, string region)
        : base(id)
    {
        TenantId = tenantId;
        Nombre = nombre;
        CodigoInterno = codigoInterno;
        Coordenadas = coordenadas;
        Region = region;
        Activo = true;
    }

    // Constructor para EF Core.
    private Centro() { }

    /// <summary>Empresa (tenant) a la que pertenece el centro.</summary>
    public Guid TenantId { get; private set; }

    public string Nombre { get; private set; } = null!;

    public string CodigoInterno { get; private set; } = null!;

    public CoordenadasGps Coordenadas { get; private set; } = null!;

    public string Region { get; private set; } = null!;

    public bool Activo { get; private set; }

    public static Centro Crear(Guid tenantId, string nombre, string codigoInterno, CoordenadasGps coordenadas, string region)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("El centro debe asociarse a una empresa (tenant).", nameof(tenantId));
        }

        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new ArgumentException("El nombre del centro es obligatorio.", nameof(nombre));
        }

        if (string.IsNullOrWhiteSpace(codigoInterno))
        {
            throw new ArgumentException("El código interno del centro es obligatorio.", nameof(codigoInterno));
        }

        return new Centro(Guid.NewGuid(), tenantId, nombre.Trim(), codigoInterno.Trim(), coordenadas, region?.Trim() ?? string.Empty);
    }

    public void Desactivar() => Activo = false;
}
