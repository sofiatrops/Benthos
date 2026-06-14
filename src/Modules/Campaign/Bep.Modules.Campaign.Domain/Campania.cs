using Bep.Modules.Campaign.Domain.Events;
using Bep.SharedKernel;

namespace Bep.Modules.Campaign.Domain;

/// <summary>
/// Campaña de monitoreo ambiental (M2). Agregado raíz tenant-scoped: pertenece a
/// una empresa y protege su ciclo de vida mediante una máquina de estados con
/// transiciones controladas (RF-02-003).
/// </summary>
public sealed class Campania : AggregateRoot<Guid>, ITenantScoped
{
    /// <summary>Transiciones de estado permitidas. Cerrada y Cancelada son terminales.</summary>
    private static readonly Dictionary<EstadoCampania, EstadoCampania[]> TransicionesPermitidas = new()
    {
        [EstadoCampania.Planificada] = [EstadoCampania.EnCurso, EstadoCampania.Cancelada],
        [EstadoCampania.EnCurso] = [EstadoCampania.EnRevision, EstadoCampania.Cancelada],
        [EstadoCampania.EnRevision] = [EstadoCampania.Cerrada, EstadoCampania.EnCurso, EstadoCampania.Cancelada],
        [EstadoCampania.Cerrada] = [],
        [EstadoCampania.Cancelada] = [],
    };

    private readonly List<Guid> _centroIds = [];
    private readonly List<Responsable> _responsables = [];

    private Campania(
        Guid id, Guid tenantId, string nombre, string descripcion,
        TipoCampania tipo, RangoFechas periodo, IEnumerable<Guid> centroIds) : base(id)
    {
        TenantId = tenantId;
        Nombre = nombre;
        Descripcion = descripcion;
        Tipo = tipo;
        Periodo = periodo;
        Estado = EstadoCampania.Planificada;
        _centroIds.AddRange(centroIds);
    }

    // Constructor para EF Core.
    private Campania() { }

    public Guid TenantId { get; private set; }

    public string Nombre { get; private set; } = null!;

    public string Descripcion { get; private set; } = null!;

    public TipoCampania Tipo { get; private set; }

    public RangoFechas Periodo { get; private set; } = null!;

    public EstadoCampania Estado { get; private set; }

    public IReadOnlyList<Guid> CentroIds => _centroIds.AsReadOnly();

    public IReadOnlyList<Responsable> Responsables => _responsables.AsReadOnly();

    public static Campania Crear(
        Guid empresaId, string nombre, string descripcion,
        TipoCampania tipo, RangoFechas periodo, IEnumerable<Guid> centroIds)
    {
        if (empresaId == Guid.Empty)
        {
            throw new ArgumentException("La campaña debe asociarse a una empresa.", nameof(empresaId));
        }

        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new ArgumentException("El nombre de la campaña es obligatorio.", nameof(nombre));
        }

        var centros = centroIds?.Distinct().ToList() ?? [];
        if (centros.Count == 0)
        {
            throw new ArgumentException("La campaña debe asociar al menos un centro.", nameof(centroIds));
        }

        var campania = new Campania(Guid.NewGuid(), empresaId, nombre.Trim(), descripcion?.Trim() ?? string.Empty, tipo, periodo, centros);
        campania.RaiseDomainEvent(new CampanaCreada(campania.Id, empresaId, campania.Nombre));
        return campania;
    }

    /// <summary>Asigna (reemplaza) los responsables de la campaña (RF-02-002).</summary>
    public void AsignarResponsables(IEnumerable<Responsable> responsables)
    {
        _responsables.Clear();
        _responsables.AddRange(responsables.Distinct());
    }

    public bool PuedeTransicionarA(EstadoCampania nuevoEstado)
        => TransicionesPermitidas[Estado].Contains(nuevoEstado);

    /// <summary>Transiciona el estado validando la máquina de estados (RF-02-003).</summary>
    public void Transicionar(EstadoCampania nuevoEstado)
    {
        if (!PuedeTransicionarA(nuevoEstado))
        {
            throw new InvalidOperationException(
                $"Transición de estado no permitida: {Estado} → {nuevoEstado}.");
        }

        var anterior = Estado;
        Estado = nuevoEstado;

        RaiseDomainEvent(new EstadoCampanaCambiado(Id, TenantId, anterior, nuevoEstado));

        if (nuevoEstado == EstadoCampania.Cerrada)
        {
            RaiseDomainEvent(new CampanaCerrada(Id, TenantId));
        }
    }
}
