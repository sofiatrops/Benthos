using Bep.Application.Abstractions;
using Bep.Modules.Insights.Application.Abstractions;
using Bep.Modules.Insights.Application.Analisis;
using Bep.Modules.Insights.Domain;
using Microsoft.EntityFrameworkCore;

namespace Bep.Modules.Insights.Infrastructure.Persistence;

internal sealed class InsightsReadService(InsightsDbContext dbContext) : IInsightsReadService
{
    public async Task<AnalisisDto?> GetAnalisisAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var analisis = await dbContext.Set<AnalisisAmbiental>().AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        return analisis is null ? null : ToDto(analisis);
    }

    public async Task<PagedResult<AnalisisResumenDto>> ListAnalisisAsync(
        Guid empresaId, Guid? campanaId, EstadoAnalisis? estado, PageRequest page, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Set<AnalisisAmbiental>().AsNoTracking().Where(a => a.TenantId == empresaId);

        if (campanaId is { } c)
        {
            query = query.Where(a => a.CampanaId == c);
        }

        if (estado is { } e)
        {
            query = query.Where(a => a.Estado == e);
        }

        var total = await query.LongCountAsync(cancellationToken);

        var rows = await query
            .OrderByDescending(a => a.GeneradoUtc)
            .Skip(page.Skip)
            .Take(page.NormalizedPageSize)
            .Select(a => new
            {
                a.Id,
                a.CampanaId,
                a.Estado,
                a.Modelo,
                a.GeneradoUtc,
                Cantidad = a.Hallazgos.Count,
            })
            .ToListAsync(cancellationToken);

        var items = rows
            .Select(r => new AnalisisResumenDto(r.Id, r.CampanaId, r.Estado.ToString(), r.Modelo, r.GeneradoUtc, r.Cantidad))
            .ToList();

        return new PagedResult<AnalisisResumenDto>(items, page.NormalizedPage, page.NormalizedPageSize, total);
    }

    public async Task<AnalisisDto?> GetUltimoValidadoAsync(Guid empresaId, CancellationToken cancellationToken = default)
    {
        var analisis = await dbContext.Set<AnalisisAmbiental>().AsNoTracking()
            .Where(a => a.TenantId == empresaId && a.Estado == EstadoAnalisis.Validado)
            .OrderByDescending(a => a.ValidadoUtc)
            .FirstOrDefaultAsync(cancellationToken);
        return analisis is null ? null : ToDto(analisis);
    }

    private static AnalisisDto ToDto(AnalisisAmbiental a)
        => new(
            a.Id,
            a.CampanaId,
            a.Estado.ToString(),
            a.Resumen,
            a.Modelo,
            a.GeneradoUtc,
            a.ValidadoUtc,
            a.ValidadoPorSubjectId,
            a.MotivoDescarte,
            a.Hallazgos
                .Select(h => new HallazgoDto(h.Parametro, h.Severidad.ToString(), h.Detalle))
                .ToList());
}
