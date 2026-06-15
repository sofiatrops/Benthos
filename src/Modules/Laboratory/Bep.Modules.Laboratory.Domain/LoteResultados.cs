using Bep.Modules.Laboratory.Domain.Events;
using Bep.SharedKernel;

namespace Bep.Modules.Laboratory.Domain;

/// <summary>
/// Lote de resultados de laboratorio asociado a una campaña (RF-04-001). Es la raíz
/// del agregado: agrupa las mediciones de parámetros (<see cref="ResultadoParametro"/>)
/// de una entrega del laboratorio y gobierna su ciclo de validación. Solo un lote
/// <see cref="EstadoLote.Validado"/> alimenta los indicadores del portal (RF-04-005).
/// </summary>
public sealed class LoteResultados : AggregateRoot<Guid>, ITenantScoped
{
    private readonly List<ResultadoParametro> _resultados = [];

    private LoteResultados(Guid id, Guid tenantId, Guid campanaId, string laboratorio, string archivoObjectKey)
        : base(id)
    {
        TenantId = tenantId;
        CampanaId = campanaId;
        Laboratorio = laboratorio;
        ArchivoObjectKey = archivoObjectKey;
        Estado = EstadoLote.Recibido;
        RecibidoUtc = DateTimeOffset.UtcNow;
    }

    // Constructor para EF Core.
    private LoteResultados() { }

    public Guid TenantId { get; private set; }

    public Guid CampanaId { get; private set; }

    public string Laboratorio { get; private set; } = null!;

    /// <summary>Clave del archivo original (CSV/Excel) conservado para auditoría.</summary>
    public string ArchivoObjectKey { get; private set; } = null!;

    public EstadoLote Estado { get; private set; }

    public DateTimeOffset RecibidoUtc { get; private set; }

    public DateTimeOffset? ValidadoUtc { get; private set; }

    public string? MotivoRechazo { get; private set; }

    public IReadOnlyList<ResultadoParametro> Resultados => _resultados.AsReadOnly();

    public static LoteResultados Recibir(
        Guid tenantId, Guid campanaId, string laboratorio, string archivoObjectKey,
        IReadOnlyCollection<ResultadoParametro> resultados)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("El lote debe asociarse a una empresa (tenant).", nameof(tenantId));
        }

        if (campanaId == Guid.Empty)
        {
            throw new ArgumentException("El lote debe asociarse a una campaña.", nameof(campanaId));
        }

        if (string.IsNullOrWhiteSpace(laboratorio))
        {
            throw new ArgumentException("El laboratorio emisor es obligatorio.", nameof(laboratorio));
        }

        if (resultados is null || resultados.Count == 0)
        {
            throw new ArgumentException("El lote debe contener al menos un resultado.", nameof(resultados));
        }

        var lote = new LoteResultados(Guid.NewGuid(), tenantId, campanaId, laboratorio.Trim(), archivoObjectKey ?? string.Empty);
        lote._resultados.AddRange(resultados);
        lote.RaiseDomainEvent(new LoteResultadosRecibido(lote.Id, tenantId, campanaId, lote._resultados.Count));
        return lote;
    }

    /// <summary>Valida el lote (revisión profesional, RF-04-005). Solo desde <see cref="EstadoLote.Recibido"/>.</summary>
    public void Validar()
    {
        if (Estado != EstadoLote.Recibido)
        {
            throw new InvalidOperationException($"Solo se puede validar un lote en estado Recibido (actual: {Estado}).");
        }

        Estado = EstadoLote.Validado;
        ValidadoUtc = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new LoteResultadosValidado(Id, TenantId, CampanaId, _resultados.Count));
    }

    /// <summary>Rechaza el lote por inconsistencias. Solo desde <see cref="EstadoLote.Recibido"/>.</summary>
    public void Rechazar(string motivo)
    {
        if (Estado != EstadoLote.Recibido)
        {
            throw new InvalidOperationException($"Solo se puede rechazar un lote en estado Recibido (actual: {Estado}).");
        }

        if (string.IsNullOrWhiteSpace(motivo))
        {
            throw new ArgumentException("El motivo de rechazo es obligatorio.", nameof(motivo));
        }

        Estado = EstadoLote.Rechazado;
        MotivoRechazo = motivo.Trim();
        RaiseDomainEvent(new LoteResultadosRechazado(Id, TenantId, motivo.Trim()));
    }
}
