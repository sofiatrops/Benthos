using Bep.Application.Abstractions;
using Bep.Application.Abstractions.Messaging;
using Bep.Modules.Campaign.Application.Abstractions;
using Bep.Modules.Campaign.Domain;

namespace Bep.Modules.Campaign.Application.Campanias.TransicionarEstado;

/// <summary>Transiciona el estado de una campaña según su máquina de estados (RF-02-003).</summary>
public sealed record TransicionarEstadoCommand(Guid EmpresaId, Guid CampanaId, EstadoCampania NuevoEstado) : ICommand;

internal sealed class TransicionarEstadoHandler(
    ICampaniaRepository repository,
    ITenantContext tenantContext,
    ICampaignUnitOfWork unitOfWork)
    : ICommandHandler<TransicionarEstadoCommand>
{
    public async Task<Result> Handle(TransicionarEstadoCommand command, CancellationToken cancellationToken)
    {
        tenantContext.SetTenant(command.EmpresaId);

        var campania = await repository.GetByIdAsync(command.CampanaId, cancellationToken);
        if (campania is null)
        {
            return Result.Failure(Error.NotFound(
                "campaign.campania.no_encontrada", $"No existe la campaña {command.CampanaId}."));
        }

        if (!campania.PuedeTransicionarA(command.NuevoEstado))
        {
            return Result.Failure(Error.Conflict(
                "campaign.campania.transicion_invalida",
                $"Transición no permitida: {campania.Estado} → {command.NuevoEstado}."));
        }

        campania.Transicionar(command.NuevoEstado);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
