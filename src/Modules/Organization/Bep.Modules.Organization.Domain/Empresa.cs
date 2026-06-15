using Bep.Modules.Organization.Domain.Events;
using Bep.SharedKernel;

namespace Bep.Modules.Organization.Domain;

/// <summary>
/// Empresa cliente. Es el <b>tenant</b> de la plataforma: su <see cref="Entity{TId}.Id"/>
/// es el <c>tenant_id</c> al que se asocian centros, campañas, muestras e informes.
/// La administra el Super Administrador de Benthos (RF-01-001, RF-01-007).
/// </summary>
public sealed class Empresa : AggregateRoot<Guid>
{
    private readonly List<Centro> _centros = [];

    private Empresa(Guid id, string razonSocial, Rut rut, string rubro) : base(id)
    {
        RazonSocial = razonSocial;
        Rut = rut;
        Rubro = rubro;
        Activa = true;
        CreadaUtc = DateTimeOffset.UtcNow;
    }

    // Constructor para EF Core.
    private Empresa() { }

    public string RazonSocial { get; private set; } = null!;

    public Rut Rut { get; private set; } = null!;

    public string Rubro { get; private set; } = null!;

    public bool Activa { get; private set; }

    public DateTimeOffset CreadaUtc { get; private set; }

    public IReadOnlyList<Centro> Centros => _centros.AsReadOnly();

    public static Empresa Registrar(string razonSocial, Rut rut, string rubro)
        => Crear(Guid.NewGuid(), razonSocial, rut, rubro);

    /// <summary>
    /// Aprovisiona una empresa con identidad <b>preasignada</b>. Para escenarios en
    /// que el <c>tenant_id</c> se acuña fuera del dominio antes de existir la empresa
    /// (integración con el IdP / plano de control, migraciones, semillas de entorno).
    /// </summary>
    public static Empresa Provisionar(Guid id, string razonSocial, Rut rut, string rubro)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("La identidad preasignada de la empresa es obligatoria.", nameof(id));
        }

        return Crear(id, razonSocial, rut, rubro);
    }

    private static Empresa Crear(Guid id, string razonSocial, Rut rut, string rubro)
    {
        if (string.IsNullOrWhiteSpace(razonSocial))
        {
            throw new ArgumentException("La razón social es obligatoria.", nameof(razonSocial));
        }

        var empresa = new Empresa(id, razonSocial.Trim(), rut, rubro?.Trim() ?? string.Empty);
        empresa.RaiseDomainEvent(new EmpresaRegistrada(empresa.Id, empresa.RazonSocial));
        return empresa;
    }

    /// <summary>Desactiva la empresa sin eliminarla físicamente (RF-01-007).</summary>
    public void Desactivar() => Activa = false;

    public Centro AgregarCentro(string nombre, string codigoInterno, CoordenadasGps coordenadas, string region)
    {
        var centro = Centro.Crear(Id, nombre, codigoInterno, coordenadas, region);
        _centros.Add(centro);
        return centro;
    }
}
