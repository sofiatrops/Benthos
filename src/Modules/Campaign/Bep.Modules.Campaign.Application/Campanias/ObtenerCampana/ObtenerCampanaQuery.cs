using Bep.Application.Abstractions;
using Bep.Application.Abstractions.Messaging;
using Bep.Modules.Campaign.Application.Abstractions;

namespace Bep.Modules.Campaign.Application.Campanias.ObtenerCampana;

public sealed record ObtenerCampanaQuery(Guid EmpresaId, Guid CampanaId) : IQuery<CampaniaDto>;

internal sealed class ObtenerCampanaHandler(
    ICampaignReadService readService,
    ITenantContext tenantContext)
    : IQueryHandler<ObtenerCampanaQuery, CampaniaDto>
{
    public async Task<Result<CampaniaDto>> Handle(ObtenerCampanaQuery query, CancellationToken cancellationToken)
    {
        tenantContext.SetTenant(query.EmpresaId);

        var campania = await readService.GetCampaniaAsync(query.CampanaId, cancellationToken);
        return campania is null
            ? Result.Failure<CampaniaDto>(Error.NotFound(
                "campaign.campania.no_encontrada", $"No existe la campaña {query.CampanaId}."))
            : Result.Success(campania);
    }
}
