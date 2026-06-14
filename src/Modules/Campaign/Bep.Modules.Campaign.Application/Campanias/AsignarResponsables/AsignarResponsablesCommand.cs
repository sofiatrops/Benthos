using Bep.Application.Abstractions;
using Bep.Application.Abstractions.Messaging;
using Bep.Modules.Campaign.Application.Abstractions;
using Bep.Modules.Campaign.Domain;
using FluentValidation;

namespace Bep.Modules.Campaign.Application.Campanias.AsignarResponsables;

/// <summary>Asigna (reemplaza) los responsables de una campaña (RF-02-002).</summary>
public sealed record AsignarResponsablesCommand(
    Guid EmpresaId, Guid CampanaId, IReadOnlyList<ResponsableInput> Responsables) : ICommand;

public sealed record ResponsableInput(string SubjectId, string Rol);

public sealed class AsignarResponsablesValidator : AbstractValidator<AsignarResponsablesCommand>
{
    public AsignarResponsablesValidator()
    {
        RuleFor(c => c.EmpresaId).NotEmpty();
        RuleFor(c => c.CampanaId).NotEmpty();
        RuleForEach(c => c.Responsables).ChildRules(r =>
        {
            r.RuleFor(x => x.SubjectId).NotEmpty();
            r.RuleFor(x => x.Rol).NotEmpty();
        });
    }
}

internal sealed class AsignarResponsablesHandler(
    ICampaniaRepository repository,
    ITenantContext tenantContext,
    ICampaignUnitOfWork unitOfWork)
    : ICommandHandler<AsignarResponsablesCommand>
{
    public async Task<Result> Handle(AsignarResponsablesCommand command, CancellationToken cancellationToken)
    {
        tenantContext.SetTenant(command.EmpresaId);

        var campania = await repository.GetByIdAsync(command.CampanaId, cancellationToken);
        if (campania is null)
        {
            return Result.Failure(Error.NotFound(
                "campaign.campania.no_encontrada", $"No existe la campaña {command.CampanaId}."));
        }

        var responsables = command.Responsables.Select(r => Responsable.Create(r.SubjectId, r.Rol));
        campania.AsignarResponsables(responsables);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
