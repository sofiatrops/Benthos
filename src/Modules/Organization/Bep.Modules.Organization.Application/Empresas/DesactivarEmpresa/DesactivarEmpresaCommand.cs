using Bep.Application.Abstractions;
using Bep.Application.Abstractions.Messaging;
using Bep.Modules.Organization.Application.Abstractions;

namespace Bep.Modules.Organization.Application.Empresas.DesactivarEmpresa;

/// <summary>Desactiva (no elimina) una empresa. RF-01-007.</summary>
public sealed record DesactivarEmpresaCommand(Guid EmpresaId) : ICommand;

internal sealed class DesactivarEmpresaHandler(
    IEmpresaRepository empresaRepository,
    IOrganizationUnitOfWork unitOfWork)
    : ICommandHandler<DesactivarEmpresaCommand>
{
    public async Task<Result> Handle(DesactivarEmpresaCommand command, CancellationToken cancellationToken)
    {
        var empresa = await empresaRepository.GetByIdAsync(command.EmpresaId, cancellationToken);
        if (empresa is null)
        {
            return Result.Failure(Error.NotFound(
                "organization.empresa.no_encontrada",
                $"No existe la empresa {command.EmpresaId}."));
        }

        empresa.Desactivar();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
