using Bep.Modules.Insights.Domain.Events;
using Bep.SharedKernel;

namespace Bep.Modules.Insights.Domain;

/// <summary>
/// Análisis ambiental asistido por IA sobre los resultados validados de una campaña
/// (RF-06). Raíz del agregado. Se genera como <see cref="EstadoAnalisis.Borrador"/> y
/// <b>requiere validación profesional</b> antes de poder mostrarse al cliente
/// (humano en el bucle, RF-06-007/010). Conserva el modelo que lo produjo (trazabilidad).
/// </summary>
public sealed class AnalisisAmbiental : AggregateRoot<Guid>, ITenantScoped
{
    private readonly List<Hallazgo> _hallazgos = [];

    private AnalisisAmbiental(Guid id, Guid tenantId, Guid campanaId, string resumen, string modelo)
        : base(id)
    {
        TenantId = tenantId;
        CampanaId = campanaId;
        Resumen = resumen;
        Modelo = modelo;
        Estado = EstadoAnalisis.Borrador;
        GeneradoUtc = DateTimeOffset.UtcNow;
    }

    // Constructor para EF Core.
    private AnalisisAmbiental() { }

    public Guid TenantId { get; private set; }

    public Guid CampanaId { get; private set; }

    public string Resumen { get; private set; } = null!;

    /// <summary>Modelo/proveedor que generó el análisis (p. ej. <c>deterministic-v1</c>, <c>claude-sonnet-4-6</c>).</summary>
    public string Modelo { get; private set; } = null!;

    public EstadoAnalisis Estado { get; private set; }

    public DateTimeOffset GeneradoUtc { get; private set; }

    public DateTimeOffset? ValidadoUtc { get; private set; }

    public string? ValidadoPorSubjectId { get; private set; }

    public string? MotivoDescarte { get; private set; }

    public IReadOnlyList<Hallazgo> Hallazgos => _hallazgos.AsReadOnly();

    public static AnalisisAmbiental Generar(
        Guid tenantId, Guid campanaId, string resumen, string modelo, IEnumerable<Hallazgo> hallazgos)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("El análisis debe asociarse a una empresa (tenant).", nameof(tenantId));
        }

        if (campanaId == Guid.Empty)
        {
            throw new ArgumentException("El análisis debe asociarse a una campaña.", nameof(campanaId));
        }

        if (string.IsNullOrWhiteSpace(resumen))
        {
            throw new ArgumentException("El análisis requiere un resumen.", nameof(resumen));
        }

        if (string.IsNullOrWhiteSpace(modelo))
        {
            throw new ArgumentException("Se debe registrar el modelo que generó el análisis.", nameof(modelo));
        }

        var analisis = new AnalisisAmbiental(Guid.NewGuid(), tenantId, campanaId, resumen.Trim(), modelo.Trim());
        analisis._hallazgos.AddRange(hallazgos);
        analisis.RaiseDomainEvent(new AnalisisGenerado(analisis.Id, tenantId, campanaId, analisis.Modelo));
        return analisis;
    }

    /// <summary>Validación profesional (RF-06-007). Solo desde <see cref="EstadoAnalisis.Borrador"/>.</summary>
    public void Validar(string validadoPorSubjectId)
    {
        if (Estado != EstadoAnalisis.Borrador)
        {
            throw new InvalidOperationException($"Solo se puede validar un análisis en estado Borrador (actual: {Estado}).");
        }

        Estado = EstadoAnalisis.Validado;
        ValidadoUtc = DateTimeOffset.UtcNow;
        ValidadoPorSubjectId = string.IsNullOrWhiteSpace(validadoPorSubjectId) ? "system" : validadoPorSubjectId;
        RaiseDomainEvent(new AnalisisValidado(Id, TenantId, CampanaId, ValidadoPorSubjectId));
    }

    /// <summary>Descarta el borrador (p. ej. interpretación incorrecta). Solo desde <see cref="EstadoAnalisis.Borrador"/>.</summary>
    public void Descartar(string motivo)
    {
        if (Estado != EstadoAnalisis.Borrador)
        {
            throw new InvalidOperationException($"Solo se puede descartar un análisis en estado Borrador (actual: {Estado}).");
        }

        if (string.IsNullOrWhiteSpace(motivo))
        {
            throw new ArgumentException("El motivo de descarte es obligatorio.", nameof(motivo));
        }

        Estado = EstadoAnalisis.Descartado;
        MotivoDescarte = motivo.Trim();
        RaiseDomainEvent(new AnalisisDescartado(Id, TenantId, motivo.Trim()));
    }
}
