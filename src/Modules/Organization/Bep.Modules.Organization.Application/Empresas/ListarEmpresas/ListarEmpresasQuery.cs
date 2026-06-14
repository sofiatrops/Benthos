using Bep.Application.Abstractions;
using Bep.Application.Abstractions.Messaging;
using Bep.Modules.Organization.Application.Abstractions;

namespace Bep.Modules.Organization.Application.Empresas.ListarEmpresas;

/// <summary>Lista empresas con filtro y paginación (RF-01-008, RNF-REND-005).</summary>
public sealed record ListarEmpresasQuery(string? Search, bool? Activa, int Page = 1, int PageSize = 20)
    : IQuery<PagedResult<EmpresaDto>>;

internal sealed class ListarEmpresasHandler(IOrganizationReadService readService)
    : IQueryHandler<ListarEmpresasQuery, PagedResult<EmpresaDto>>
{
    public async Task<Result<PagedResult<EmpresaDto>>> Handle(ListarEmpresasQuery query, CancellationToken cancellationToken)
    {
        var page = new PageRequest(query.Page, query.PageSize);
        var result = await readService.ListEmpresasAsync(query.Search, query.Activa, page, cancellationToken);
        return Result.Success(result);
    }
}
