using Bep.Application.Abstractions;
using Bep.Modules.Reporting.Application.Abstractions;
using Bep.Modules.Reporting.Application.Informes;
using Bep.Modules.Reporting.Domain;
using Microsoft.EntityFrameworkCore;

namespace Bep.Modules.Reporting.Infrastructure.Persistence;

internal sealed class ReportingReadService(ReportingDbContext dbContext) : IReportingReadService
{
    public async Task<InformeDto?> GetInformeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var informe = await dbContext.Set<Informe>().AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
        return informe is null ? null : ToDto(informe);
    }

    public Task<PagedResult<InformeResumenDto>> ListInformesAsync(
        Guid empresaId, EstadoInforme? estado, Guid? campanaId, PageRequest page, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Set<Informe>().AsNoTracking().Where(i => i.TenantId == empresaId);

        if (estado is { } e)
        {
            query = query.Where(i => i.Estado == e);
        }

        if (campanaId is { } c)
        {
            query = query.Where(i => i.CampanaId == c);
        }

        return PaginarResumenAsync(query, page, cancellationToken);
    }

    public Task<PagedResult<InformeResumenDto>> ListPublicadosAsync(
        Guid empresaId, PublicadosFilter filter, PageRequest page, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Set<Informe>().AsNoTracking()
            .Where(i => i.TenantId == empresaId && i.Estado == EstadoInforme.Publicado);

        if (filter.TipoEstudio is { } tipo)
        {
            query = query.Where(i => i.TipoEstudio == tipo);
        }

        if (filter.CentroId is { } centroId)
        {
            query = query.Where(i => i.CentroId == centroId);
        }

        if (filter.Desde is { } desde)
        {
            query = query.Where(i => i.Periodo.Hasta >= desde);
        }

        if (filter.Hasta is { } hasta)
        {
            query = query.Where(i => i.Periodo.Desde <= hasta);
        }

        return PaginarResumenAsync(query, page, cancellationToken);
    }

    private static async Task<PagedResult<InformeResumenDto>> PaginarResumenAsync(
        IQueryable<Informe> query, PageRequest page, CancellationToken cancellationToken)
    {
        var total = await query.LongCountAsync(cancellationToken);

        // Proyección a columnas escalares (sin cargar las colecciones poseídas).
        var rows = await query
            .OrderByDescending(i => i.CreadoUtc)
            .Skip(page.Skip)
            .Take(page.NormalizedPageSize)
            .Select(i => new
            {
                i.Id,
                i.Titulo,
                i.TipoEstudio,
                i.Estado,
                Desde = i.Periodo.Desde,
                Hasta = i.Periodo.Hasta,
                i.VersionVigenteNumero,
                i.CreadoUtc,
            })
            .ToListAsync(cancellationToken);

        var items = rows
            .Select(r => new InformeResumenDto(
                r.Id, r.Titulo, r.TipoEstudio.ToString(), r.Estado.ToString(),
                r.Desde, r.Hasta, r.VersionVigenteNumero, r.CreadoUtc))
            .ToList();

        return new PagedResult<InformeResumenDto>(items, page.NormalizedPage, page.NormalizedPageSize, total);
    }

    private static InformeDto ToDto(Informe i)
        => new(
            i.Id,
            i.TenantId,
            i.Titulo,
            i.TipoEstudio.ToString(),
            i.Periodo.Desde,
            i.Periodo.Hasta,
            i.CampanaId,
            i.CentroId,
            i.AutorSubjectId,
            i.Estado.ToString(),
            i.CreadoUtc,
            i.FechaAprobacionUtc,
            i.VersionVigenteNumero,
            i.Versiones
                .OrderBy(v => v.Numero)
                .Select(v => new VersionInformeDto(v.Numero, v.ObjectKey, v.FechaCargaUtc, v.CargadoPorSubjectId))
                .ToList(),
            i.Comentarios
                .OrderBy(c => c.FechaUtc)
                .Select(c => new ComentarioInternoDto(c.AutorSubjectId, c.Texto, c.FechaUtc))
                .ToList(),
            i.Anexos
                .OrderBy(a => a.FechaUtc)
                .Select(a => new AnexoDto(a.ObjectKey, a.Descripcion, a.FechaUtc))
                .ToList());
}
