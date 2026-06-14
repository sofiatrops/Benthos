using Bep.Modules.Reporting.Domain.Events;
using Bep.SharedKernel;

namespace Bep.Modules.Reporting.Domain;

/// <summary>
/// Informe técnico (M5). Agregado raíz tenant-scoped que gobierna el versionado
/// (RF-05-002), el flujo de revisión/aprobación/publicación (RF-05-003) y la
/// inmutabilidad de los informes publicados (RF-05-010).
/// </summary>
public sealed class Informe : AggregateRoot<Guid>, ITenantScoped
{
    /// <summary>Transiciones del flujo de revisión. El archivado se maneja aparte (rol admin).</summary>
    private static readonly Dictionary<EstadoInforme, EstadoInforme[]> TransicionesPermitidas = new()
    {
        [EstadoInforme.Borrador] = [EstadoInforme.EnRevision],
        [EstadoInforme.EnRevision] = [EstadoInforme.CambiosSolicitados, EstadoInforme.Aprobado],
        [EstadoInforme.CambiosSolicitados] = [EstadoInforme.EnRevision],
        [EstadoInforme.Aprobado] = [EstadoInforme.Publicado],
        [EstadoInforme.Publicado] = [],
        [EstadoInforme.Archivado] = [],
    };

    private readonly List<VersionInforme> _versiones = [];
    private readonly List<ComentarioInterno> _comentarios = [];
    private readonly List<Anexo> _anexos = [];

    private Informe(
        Guid id, Guid tenantId, string titulo, TipoEstudio tipoEstudio, PeriodoCubierto periodo,
        Guid? campanaId, Guid? centroId, string autorSubjectId) : base(id)
    {
        TenantId = tenantId;
        Titulo = titulo;
        TipoEstudio = tipoEstudio;
        Periodo = periodo;
        CampanaId = campanaId;
        CentroId = centroId;
        AutorSubjectId = autorSubjectId;
        Estado = EstadoInforme.Borrador;
        CreadoUtc = DateTimeOffset.UtcNow;
    }

    // Constructor para EF Core.
    private Informe() { }

    public Guid TenantId { get; private set; }

    public string Titulo { get; private set; } = null!;

    public TipoEstudio TipoEstudio { get; private set; }

    public PeriodoCubierto Periodo { get; private set; } = null!;

    public Guid? CampanaId { get; private set; }

    public Guid? CentroId { get; private set; }

    public string AutorSubjectId { get; private set; } = null!;

    public EstadoInforme Estado { get; private set; }

    public DateTimeOffset CreadoUtc { get; private set; }

    public DateTimeOffset? FechaAprobacionUtc { get; private set; }

    /// <summary>Número de la versión vigente del informe (RF-05-007).</summary>
    public int VersionVigenteNumero { get; private set; }

    public IReadOnlyList<VersionInforme> Versiones => _versiones.AsReadOnly();

    public IReadOnlyList<ComentarioInterno> Comentarios => _comentarios.AsReadOnly();

    public IReadOnlyList<Anexo> Anexos => _anexos.AsReadOnly();

    public bool EsVisibleParaCliente => Estado == EstadoInforme.Publicado;

    public static Informe Crear(
        Guid empresaId, string titulo, TipoEstudio tipoEstudio, PeriodoCubierto periodo,
        Guid? campanaId, Guid? centroId, string autorSubjectId, string primeraVersionObjectKey)
    {
        if (empresaId == Guid.Empty)
        {
            throw new ArgumentException("El informe debe asociarse a una empresa.", nameof(empresaId));
        }

        if (string.IsNullOrWhiteSpace(titulo))
        {
            throw new ArgumentException("El título del informe es obligatorio.", nameof(titulo));
        }

        if (string.IsNullOrWhiteSpace(primeraVersionObjectKey))
        {
            throw new ArgumentException("El informe requiere el PDF de su primera versión.", nameof(primeraVersionObjectKey));
        }

        var informe = new Informe(
            Guid.NewGuid(), empresaId, titulo.Trim(), tipoEstudio, periodo, campanaId, centroId,
            autorSubjectId);

        informe.AgregarVersion(primeraVersionObjectKey, autorSubjectId);
        informe.RaiseDomainEvent(new InformeCreado(informe.Id, empresaId, informe.Titulo));
        return informe;
    }

    /// <summary>Carga una nueva versión del PDF, conservando las anteriores (RF-05-002).</summary>
    public void CargarVersion(string objectKey, string? cargadoPorSubjectId)
    {
        if (Estado is EstadoInforme.Publicado or EstadoInforme.Archivado)
        {
            throw new InvalidOperationException("No se pueden cargar versiones de un informe publicado o archivado.");
        }

        var version = AgregarVersion(objectKey, cargadoPorSubjectId);
        RaiseDomainEvent(new VersionInformeCargada(Id, TenantId, version.Numero));
    }

    public void AgregarComentarioInterno(string autorSubjectId, string texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            throw new ArgumentException("El comentario no puede estar vacío.", nameof(texto));
        }

        _comentarios.Add(new ComentarioInterno(autorSubjectId, texto.Trim(), DateTimeOffset.UtcNow));
    }

    public void AgregarAnexo(string objectKey, string descripcion)
    {
        if (string.IsNullOrWhiteSpace(objectKey))
        {
            throw new ArgumentException("La referencia del anexo es obligatoria.", nameof(objectKey));
        }

        _anexos.Add(new Anexo(objectKey.Trim(), descripcion?.Trim() ?? string.Empty, DateTimeOffset.UtcNow));
    }

    public bool PuedeTransicionarA(EstadoInforme nuevoEstado)
        => TransicionesPermitidas[Estado].Contains(nuevoEstado);

    /// <summary>Avanza el flujo de revisión/publicación (RF-05-003).</summary>
    public void Transicionar(EstadoInforme nuevoEstado)
    {
        if (!PuedeTransicionarA(nuevoEstado))
        {
            throw new InvalidOperationException($"Transición de estado no permitida: {Estado} → {nuevoEstado}.");
        }

        var anterior = Estado;
        Estado = nuevoEstado;

        if (nuevoEstado == EstadoInforme.Aprobado)
        {
            FechaAprobacionUtc = DateTimeOffset.UtcNow;
        }

        RaiseDomainEvent(new EstadoInformeCambiado(Id, TenantId, anterior, nuevoEstado));

        if (nuevoEstado == EstadoInforme.Publicado)
        {
            RaiseDomainEvent(new InformePublicado(Id, TenantId));
        }
    }

    /// <summary>Archiva (eliminación lógica) el informe; única forma de "eliminar" uno publicado (RF-05-010).</summary>
    public void Archivar()
    {
        if (Estado == EstadoInforme.Archivado)
        {
            throw new InvalidOperationException("El informe ya está archivado.");
        }

        var anterior = Estado;
        Estado = EstadoInforme.Archivado;
        RaiseDomainEvent(new EstadoInformeCambiado(Id, TenantId, anterior, EstadoInforme.Archivado));
        RaiseDomainEvent(new InformeArchivado(Id, TenantId));
    }

    private VersionInforme AgregarVersion(string objectKey, string? cargadoPorSubjectId)
    {
        if (string.IsNullOrWhiteSpace(objectKey))
        {
            throw new ArgumentException("La referencia del PDF es obligatoria.", nameof(objectKey));
        }

        var numero = _versiones.Count == 0 ? 1 : _versiones.Max(v => v.Numero) + 1;
        var version = new VersionInforme(numero, objectKey.Trim(), DateTimeOffset.UtcNow, cargadoPorSubjectId);
        _versiones.Add(version);
        VersionVigenteNumero = numero;
        return version;
    }
}
