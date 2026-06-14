using Bep.Application.Abstractions;
using Bep.Application.Abstractions.Messaging;
using Bep.Modules.Campaign.Application.Abstractions;
using Bep.Modules.Campaign.Domain;
using FluentValidation;

namespace Bep.Modules.Campaign.Application.Campanias.CrearCampana;

/// <summary>Crea una campaña de monitoreo en estado Planificada (RF-02-001).</summary>
public sealed record CrearCampanaCommand(
    Guid EmpresaId,
    string Nombre,
    string Descripcion,
    TipoCampania Tipo,
    DateOnly FechaInicio,
    DateOnly FechaFin,
    IReadOnlyList<Guid> CentroIds) : ICommand<Guid>;

public sealed class CrearCampanaValidator : AbstractValidator<CrearCampanaCommand>
{
    public CrearCampanaValidator()
    {
        RuleFor(c => c.EmpresaId).NotEmpty();
        RuleFor(c => c.Nombre).NotEmpty().MaximumLength(300);
        RuleFor(c => c.Descripcion).MaximumLength(2000);
        RuleFor(c => c.Tipo).IsInEnum();
        RuleFor(c => c.CentroIds).NotEmpty().WithMessage("La campaña debe asociar al menos un centro.");
        RuleFor(c => c.FechaFin)
            .GreaterThanOrEqualTo(c => c.FechaInicio)
            .WithMessage("La fecha de fin no puede ser anterior a la de inicio.");
    }
}

internal sealed class CrearCampanaHandler(
    ICampaniaRepository repository,
    ITenantContext tenantContext,
    ICampaignUnitOfWork unitOfWork)
    : ICommandHandler<CrearCampanaCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CrearCampanaCommand command, CancellationToken cancellationToken)
    {
        tenantContext.SetTenant(command.EmpresaId);

        var periodo = RangoFechas.Create(command.FechaInicio, command.FechaFin);
        var campania = Campania.Crear(
            command.EmpresaId, command.Nombre, command.Descripcion, command.Tipo, periodo, command.CentroIds);

        await repository.AddAsync(campania, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(campania.Id);
    }
}
