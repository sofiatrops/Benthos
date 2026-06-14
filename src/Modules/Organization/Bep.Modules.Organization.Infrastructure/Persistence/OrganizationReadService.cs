using Bep.Application.Abstractions;
using Bep.Modules.Organization.Application.Abstractions;
using Bep.Modules.Organization.Application.Centros;
using Bep.Modules.Organization.Application.Empresas;
using Bep.Modules.Organization.Domain;
using Microsoft.EntityFrameworkCore;

namespace Bep.Modules.Organization.Infrastructure.Persistence;

internal sealed class OrganizationReadService(OrganizationDbContext dbContext) : IOrganizationReadService
{
    public async Task<EmpresaDto?> GetEmpresaAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var empresa = await dbContext.Set<Empresa>()
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        return empresa is null ? null : ToDto(empresa);
    }

    public async Task<PagedResult<EmpresaDto>> ListEmpresasAsync(
        string? search, bool? activa, PageRequest page, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Set<Empresa>().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(e => EF.Functions.ILike(e.RazonSocial, pattern));
        }

        if (activa.HasValue)
        {
            query = query.Where(e => e.Activa == activa.Value);
        }

        var total = await query.LongCountAsync(cancellationToken);

        // El value object Rut se materializa vía su conversor y se mapea en memoria
        // (solo los elementos de la página).
        var empresas = await query
            .OrderBy(e => e.RazonSocial)
            .Skip(page.Skip)
            .Take(page.NormalizedPageSize)
            .ToListAsync(cancellationToken);

        var items = empresas.Select(ToDto).ToList();

        return new PagedResult<EmpresaDto>(items, page.NormalizedPage, page.NormalizedPageSize, total);
    }

    public async Task<PagedResult<CentroDto>> ListCentrosAsync(
        Guid empresaId, PageRequest page, CancellationToken cancellationToken = default)
    {
        // La RLS restringe por tenant; el filtro por empresaId es explícito y redundante.
        var query = dbContext.Set<Centro>().AsNoTracking().Where(c => c.TenantId == empresaId);

        var total = await query.LongCountAsync(cancellationToken);

        var items = await query
            .OrderBy(c => c.Nombre)
            .Skip(page.Skip)
            .Take(page.NormalizedPageSize)
            .Select(c => new CentroDto(
                c.Id, c.TenantId, c.Nombre, c.CodigoInterno,
                c.Coordenadas.Latitud, c.Coordenadas.Longitud, c.Region, c.Activo))
            .ToListAsync(cancellationToken);

        return new PagedResult<CentroDto>(items, page.NormalizedPage, page.NormalizedPageSize, total);
    }

    private static EmpresaDto ToDto(Empresa e)
        => new(e.Id, e.RazonSocial, e.Rut.Value, e.Rubro, e.Activa, e.CreadaUtc);
}
