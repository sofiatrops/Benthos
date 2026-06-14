using Bep.Modules.Sampling.Domain.Events;
using Bep.SharedKernel;

namespace Bep.Modules.Sampling.Domain;

/// <summary>
/// Muestra ambiental recolectada en terreno (M3). Agregado raíz tenant-scoped y
/// núcleo de la trazabilidad: identificador único, QR, geolocalización, historial
/// cronológico de eventos y cadena de custodia (RF-03-001..011).
/// </summary>
public sealed class Muestra : AggregateRoot<Guid>, ITenantScoped
{
    /// <summary>Transiciones de estado de laboratorio (las de custodia se manejan aparte).</summary>
    private static readonly Dictionary<EstadoMuestra, EstadoMuestra[]> TransicionesLaboratorio = new()
    {
        [EstadoMuestra.RecibidaLaboratorio] = [EstadoMuestra.EnAnalisis],
        [EstadoMuestra.EnAnalisis] = [EstadoMuestra.ConResultado],
        [EstadoMuestra.ConResultado] = [EstadoMuestra.Archivada],
    };

    private readonly List<string> _parametrosSolicitados = [];
    private readonly List<string> _fotos = [];
    private readonly List<EventoMuestra> _eventos = [];
    private readonly List<RegistroCustodia> _custodias = [];

    private Muestra(
        Guid id, Guid tenantId, Guid campanaId, Guid centroId, string codigoUnico, CodigoQr codigoQr,
        TipoMuestra tipo, UbicacionGps ubicacion, IEnumerable<string> parametros) : base(id)
    {
        TenantId = tenantId;
        CampanaId = campanaId;
        CentroId = centroId;
        CodigoUnico = codigoUnico;
        CodigoQr = codigoQr;
        Tipo = tipo;
        Ubicacion = ubicacion;
        Estado = EstadoMuestra.Registrada;
        FechaRegistroUtc = DateTimeOffset.UtcNow;
        _parametrosSolicitados.AddRange(parametros);
    }

    // Constructor para EF Core.
    private Muestra() { }

    public Guid TenantId { get; private set; }

    public Guid CampanaId { get; private set; }

    public Guid CentroId { get; private set; }

    /// <summary>Identificador único legible generado por el sistema (RF-03-001).</summary>
    public string CodigoUnico { get; private set; } = null!;

    public CodigoQr CodigoQr { get; private set; } = null!;

    public TipoMuestra Tipo { get; private set; }

    public UbicacionGps Ubicacion { get; private set; } = null!;

    public EstadoMuestra Estado { get; private set; }

    public DateTimeOffset FechaRegistroUtc { get; private set; }

    public IReadOnlyList<string> ParametrosSolicitados => _parametrosSolicitados.AsReadOnly();

    public IReadOnlyList<string> Fotos => _fotos.AsReadOnly();

    public IReadOnlyList<EventoMuestra> Eventos => _eventos.AsReadOnly();

    public IReadOnlyList<RegistroCustodia> Custodias => _custodias.AsReadOnly();

    public static Muestra Registrar(
        Guid empresaId, Guid campanaId, Guid centroId, TipoMuestra tipo,
        IEnumerable<string> parametros, UbicacionGps ubicacion, string? usuarioSubjectId)
    {
        if (empresaId == Guid.Empty)
        {
            throw new ArgumentException("La muestra debe asociarse a una empresa.", nameof(empresaId));
        }

        if (campanaId == Guid.Empty)
        {
            throw new ArgumentException("La muestra debe asociarse a una campaña (RF-03-009).", nameof(campanaId));
        }

        if (centroId == Guid.Empty)
        {
            throw new ArgumentException("La muestra debe asociarse a un centro (RF-03-009).", nameof(centroId));
        }

        var codigoUnico = GenerarCodigoUnico();
        var muestra = new Muestra(
            Guid.NewGuid(), empresaId, campanaId, centroId, codigoUnico, CodigoQr.Generar(),
            tipo, ubicacion, parametros ?? []);

        muestra.RegistrarEvento(TipoEventoMuestra.Registro, usuarioSubjectId, "Muestra registrada en terreno.");
        muestra.RaiseDomainEvent(new MuestraRegistrada(muestra.Id, empresaId, campanaId, centroId, codigoUnico));
        return muestra;
    }

    /// <summary>Adjunta la referencia de almacenamiento de una fotografía (RF-03-004).</summary>
    public void AgregarFoto(string objectKey, string? usuarioSubjectId)
    {
        if (string.IsNullOrWhiteSpace(objectKey))
        {
            throw new ArgumentException("La referencia de la foto es obligatoria.", nameof(objectKey));
        }

        _fotos.Add(objectKey.Trim());
        RegistrarEvento(TipoEventoMuestra.Fotografia, usuarioSubjectId, "Fotografía adjuntada.");
    }

    /// <summary>Inicia una transferencia de custodia, pendiente de aceptación (RF-03-007).</summary>
    public void TransferirCustodia(string? deSubjectId, string paraSubjectId, string? usuarioSubjectId)
    {
        if (Estado is EstadoMuestra.Archivada)
        {
            throw new InvalidOperationException("No se puede transferir la custodia de una muestra archivada.");
        }

        if (string.IsNullOrWhiteSpace(paraSubjectId))
        {
            throw new ArgumentException("Debe indicarse el receptor de la custodia.", nameof(paraSubjectId));
        }

        _custodias.Add(new RegistroCustodia(deSubjectId, paraSubjectId.Trim(), DateTimeOffset.UtcNow));
        CambiarEstado(EstadoMuestra.EnTraslado);
        RegistrarEvento(TipoEventoMuestra.Traslado, usuarioSubjectId, $"Custodia transferida a {paraSubjectId.Trim()}.");
        RaiseDomainEvent(new CustodiaTransferida(Id, TenantId, deSubjectId, paraSubjectId.Trim()));
    }

    /// <summary>Acepta la custodia pendiente; la muestra pasa a recibida en laboratorio (RF-03-007).</summary>
    public void AceptarCustodia(string porSubjectId)
    {
        var pendiente = _custodias.LastOrDefault(c => !c.Aceptada)
            ?? throw new InvalidOperationException("No hay una transferencia de custodia pendiente de aceptar.");

        pendiente.Aceptar(DateTimeOffset.UtcNow);
        CambiarEstado(EstadoMuestra.RecibidaLaboratorio);
        RegistrarEvento(TipoEventoMuestra.Recepcion, porSubjectId, "Custodia aceptada / muestra recibida en laboratorio.");
        RaiseDomainEvent(new CustodiaAceptada(Id, TenantId, porSubjectId));
    }

    public bool PuedeTransicionarA(EstadoMuestra nuevoEstado)
        => TransicionesLaboratorio.TryGetValue(Estado, out var permitidas) && permitidas.Contains(nuevoEstado);

    /// <summary>Avanza el estado de laboratorio (análisis, resultado, archivo) registrando el evento.</summary>
    public void Transicionar(EstadoMuestra nuevoEstado, string? usuarioSubjectId, string descripcion)
    {
        if (!PuedeTransicionarA(nuevoEstado))
        {
            throw new InvalidOperationException($"Transición de estado no permitida: {Estado} → {nuevoEstado}.");
        }

        CambiarEstado(nuevoEstado);
        RegistrarEvento(EventoParaEstado(nuevoEstado), usuarioSubjectId, descripcion);
    }

    private void CambiarEstado(EstadoMuestra nuevoEstado)
    {
        var anterior = Estado;
        Estado = nuevoEstado;
        RaiseDomainEvent(new EstadoMuestraCambiado(Id, TenantId, anterior, nuevoEstado));
    }

    private void RegistrarEvento(TipoEventoMuestra tipo, string? usuarioSubjectId, string descripcion)
        => _eventos.Add(new EventoMuestra(tipo, DateTimeOffset.UtcNow, usuarioSubjectId, descripcion));

    private static TipoEventoMuestra EventoParaEstado(EstadoMuestra estado) => estado switch
    {
        EstadoMuestra.EnAnalisis => TipoEventoMuestra.Analisis,
        EstadoMuestra.ConResultado => TipoEventoMuestra.Resultado,
        EstadoMuestra.Archivada => TipoEventoMuestra.Archivo,
        _ => TipoEventoMuestra.Registro,
    };

    private static string GenerarCodigoUnico()
        => $"MTR-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..10].ToUpperInvariant()}";
}
