using Bep.Application.Abstractions;
using Bep.Application.Abstractions.Messaging;
using Bep.Modules.Organization.Application.Abstractions;

namespace Bep.Modules.Organization.Application.Empresas.ObtenerEmpresa;

public sealed record ObtenerEmpresaQuery(Guid Id) : IQuery<EmpresaDto>;

internal sealed class ObtenerEmpresaHandler(IOrganizationReadService readService)
    : IQueryHandler<ObtenerEmpresaQuery, EmpresaDto>
{
    public async Task<Result<EmpresaDto>> Handle(ObtenerEmpresaQuery query, CancellationToken cancellationToken)
    {
        var empresa = await readService.GetEmpresaAsync(query.Id, cancellationToken);
        return empresa is null
            ? Result.Failure<EmpresaDto>(Error.NotFound(
                "organization.empresa.no_encontrada", $"No existe la empresa {query.Id}."))
            : Result.Success(empresa);
    }
}
