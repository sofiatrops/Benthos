using Bep.Application.Abstractions;
using Bep.Modules.Sampling.Application.Abstractions;
using Bep.Modules.Sampling.Application.Muestras;
using Bep.Modules.Sampling.Domain;
using Microsoft.EntityFrameworkCore;

namespace Bep.Modules.Sampling.Infrastructure.Persistence;

internal sealed class SamplingReadService(SamplingDbContext dbContext) : ISamplingReadService
{
    public async Task<MuestraDto?> GetMuestraAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var muestra = await dbContext.Set<Muestra>().AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
        return muestra is null ? null : ToDto(muestra);
    }

    public async Task<MuestraDto?> GetMuestraPorQrAsync(string codigoQr, CancellationToken cancellationToken = default)
    {
        var qr = CodigoQr.Create(codigoQr);
        var muestra = await dbContext.Set<Muestra>().AsNoTracking()
            .FirstOrDefaultAsync(m => m.CodigoQr == qr, cancellationToken);
        return muestra is null ? null : ToDto(muestra);
    }

    public async Task<PagedResult<MuestraResumenDto>> ListMuestrasAsync(
        Guid campanaId, PageRequest page, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Set<Muestra>().AsNoTracking().Where(m => m.CampanaId == campanaId);

        var total = await query.LongCountAsync(cancellationToken);

        var muestras = await query
            .OrderByDescending(m => m.FechaRegistroUtc)
            .Skip(page.Skip)
            .Take(page.NormalizedPageSize)
            .ToListAsync(cancellationToken);

        var items = muestras
            .Select(m => new MuestraResumenDto(
                m.Id, m.CodigoUnico, m.CodigoQr.Value, m.CentroId, m.Tipo.ToString(), m.Estado.ToString(), m.FechaRegistroUtc))
            .ToList();

        return new PagedResult<MuestraResumenDto>(items, page.NormalizedPage, page.NormalizedPageSize, total);
    }

    private static MuestraDto ToDto(Muestra m)
        => new(
            m.Id,
            m.TenantId,
            m.CampanaId,
            m.CentroId,
            m.CodigoUnico,
            m.CodigoQr.Value,
            m.Tipo.ToString(),
            m.Estado.ToString(),
            m.Ubicacion.Latitud,
            m.Ubicacion.Longitud,
            m.Ubicacion.PrecisionMetros,
            m.FechaRegistroUtc,
            m.ParametrosSolicitados.ToList(),
            m.Fotos.ToList(),
            m.Eventos
                .OrderBy(e => e.FechaUtc)
                .Select(e => new EventoMuestraDto(e.Tipo.ToString(), e.FechaUtc, e.UsuarioSubjectId, e.Descripcion))
                .ToList(),
            m.Custodias
                .OrderBy(c => c.FechaTransferenciaUtc)
                .Select(c => new CustodiaDto(c.DeSubjectId, c.ParaSubjectId, c.FechaTransferenciaUtc, c.Aceptada, c.FechaAceptacionUtc))
                .ToList());
}
