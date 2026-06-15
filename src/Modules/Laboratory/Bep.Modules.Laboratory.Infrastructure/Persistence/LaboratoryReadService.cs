using Bep.Application.Abstractions;
using Bep.Modules.Laboratory.Application.Abstractions;
using Bep.Modules.Laboratory.Application.LoteResultados;
using Bep.Modules.Laboratory.Domain;
using Microsoft.EntityFrameworkCore;

namespace Bep.Modules.Laboratory.Infrastructure.Persistence;

internal sealed class LaboratoryReadService(LaboratoryDbContext dbContext) : ILaboratoryReadService
{
    public async Task<LoteResultadosDto?> GetLoteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var lote = await dbContext.Set<Domain.LoteResultados>().AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
        return lote is null ? null : ToDto(lote);
    }

    public async Task<PagedResult<LoteResumenDto>> ListLotesAsync(
        Guid empresaId, Guid? campanaId, EstadoLote? estado, PageRequest page, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Set<Domain.LoteResultados>().AsNoTracking().Where(l => l.TenantId == empresaId);

        if (campanaId is { } c)
        {
            query = query.Where(l => l.CampanaId == c);
        }

        if (estado is { } e)
        {
            query = query.Where(l => l.Estado == e);
        }

        var total = await query.LongCountAsync(cancellationToken);

        var rows = await query
            .OrderByDescending(l => l.RecibidoUtc)
            .Skip(page.Skip)
            .Take(page.NormalizedPageSize)
            .Select(l => new
            {
                l.Id,
                l.CampanaId,
                l.Laboratorio,
                l.Estado,
                Cantidad = l.Resultados.Count,
                l.RecibidoUtc,
            })
            .ToListAsync(cancellationToken);

        var items = rows
            .Select(r => new LoteResumenDto(
                r.Id, r.CampanaId, r.Laboratorio, r.Estado.ToString(), r.Cantidad, r.RecibidoUtc))
            .ToList();

        return new PagedResult<LoteResumenDto>(items, page.NormalizedPage, page.NormalizedPageSize, total);
    }

    public async Task<IReadOnlyList<LaboratorioKpi>> GetKpisAsync(Guid empresaId, CancellationToken cancellationToken = default)
    {
        var validados = dbContext.Set<Domain.LoteResultados>().AsNoTracking()
            .Where(l => l.TenantId == empresaId && l.Estado == EstadoLote.Validado);

        var lotesValidados = await validados.CountAsync(cancellationToken);
        if (lotesValidados == 0)
        {
            return [];
        }

        var mediciones = await validados.SelectMany(l => l.Resultados).CountAsync(cancellationToken);
        var muestrasConResultado = await validados
            .SelectMany(l => l.Resultados)
            .Select(r => r.CodigoMuestra)
            .Distinct()
            .CountAsync(cancellationToken);

        return
        [
            new LaboratorioKpi("Lotes validados", lotesValidados, "lotes"),
            new LaboratorioKpi("Parámetros analizados", mediciones, "mediciones"),
            new LaboratorioKpi("Muestras con resultado", muestrasConResultado, "muestras"),
        ];
    }

    public async Task<IReadOnlyList<ResultadoParametroDto>> GetResultadosValidadosPorCampanaAsync(
        Guid empresaId, Guid campanaId, CancellationToken cancellationToken = default)
    {
        var rows = await dbContext.Set<Domain.LoteResultados>().AsNoTracking()
            .Where(l => l.TenantId == empresaId && l.CampanaId == campanaId && l.Estado == EstadoLote.Validado)
            .SelectMany(l => l.Resultados)
            .Select(r => new ResultadoParametroDto(r.CodigoMuestra, r.Parametro, r.Valor, r.Unidad, r.Metodo))
            .ToListAsync(cancellationToken);

        return rows;
    }

    private static LoteResultadosDto ToDto(Domain.LoteResultados l)
        => new(
            l.Id,
            l.CampanaId,
            l.Laboratorio,
            l.Estado.ToString(),
            l.RecibidoUtc,
            l.ValidadoUtc,
            l.MotivoRechazo,
            l.ArchivoObjectKey,
            l.Resultados
                .Select(r => new ResultadoParametroDto(r.CodigoMuestra, r.Parametro, r.Valor, r.Unidad, r.Metodo))
                .ToList());
}
