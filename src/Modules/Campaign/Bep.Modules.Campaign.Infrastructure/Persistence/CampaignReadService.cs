using Bep.Application.Abstractions;
using Bep.Modules.Campaign.Application.Abstractions;
using Bep.Modules.Campaign.Application.Campanias;
using Bep.Modules.Campaign.Domain;
using Microsoft.EntityFrameworkCore;

namespace Bep.Modules.Campaign.Infrastructure.Persistence;

internal sealed class CampaignReadService(CampaignDbContext dbContext) : ICampaignReadService
{
    public async Task<CampaniaDto?> GetCampaniaAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var campania = await dbContext.Set<Campania>()
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        return campania is null ? null : ToDto(campania);
    }

    public async Task<PagedResult<CampaniaDto>> ListCampaniasAsync(
        Guid empresaId, CampaniaFilter filter, PageRequest page, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Set<Campania>().AsNoTracking().Where(c => c.TenantId == empresaId);

        if (filter.Estado is { } estado)
        {
            query = query.Where(c => c.Estado == estado);
        }

        if (filter.CentroId is { } centroId)
        {
            query = query.Where(c => c.CentroIds.Contains(centroId));
        }

        if (!string.IsNullOrWhiteSpace(filter.ResponsableSubjectId))
        {
            query = query.Where(c => c.Responsables.Any(r => r.SubjectId == filter.ResponsableSubjectId));
        }

        if (filter.Desde is { } desde)
        {
            query = query.Where(c => c.Periodo.Fin >= desde);
        }

        if (filter.Hasta is { } hasta)
        {
            query = query.Where(c => c.Periodo.Inicio <= hasta);
        }

        var total = await query.LongCountAsync(cancellationToken);

        var campanias = await query
            .OrderByDescending(c => c.Periodo.Inicio)
            .Skip(page.Skip)
            .Take(page.NormalizedPageSize)
            .ToListAsync(cancellationToken);

        var items = campanias.Select(ToDto).ToList();

        return new PagedResult<CampaniaDto>(items, page.NormalizedPage, page.NormalizedPageSize, total);
    }

    private static CampaniaDto ToDto(Campania c)
        => new(
            c.Id,
            c.TenantId,
            c.Nombre,
            c.Descripcion,
            c.Tipo.ToString(),
            c.Estado.ToString(),
            c.Periodo.Inicio,
            c.Periodo.Fin,
            c.CentroIds.ToList(),
            c.Responsables.Select(r => new ResponsableDto(r.SubjectId, r.Rol)).ToList());
}
