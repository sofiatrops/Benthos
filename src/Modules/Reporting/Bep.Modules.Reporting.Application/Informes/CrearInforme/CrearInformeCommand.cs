using Bep.Application.Abstractions;
using Bep.Application.Abstractions.Messaging;
using Bep.Application.Abstractions.Storage;
using Bep.Modules.Reporting.Application.Abstractions;
using Bep.Modules.Reporting.Domain;
using FluentValidation;

namespace Bep.Modules.Reporting.Application.Informes.CrearInforme;

/// <summary>Crea un informe en estado Borrador con su primera versión PDF (RF-05-001/006).</summary>
public sealed record CrearInformeCommand(
    Guid EmpresaId,
    string Titulo,
    TipoEstudio TipoEstudio,
    DateOnly PeriodoDesde,
    DateOnly PeriodoHasta,
    Guid? CampanaId,
    Guid? CentroId,
    string PrimeraVersionObjectKey) : ICommand<Guid>;

public sealed class CrearInformeValidator : AbstractValidator<CrearInformeCommand>
{
    public CrearInformeValidator()
    {
        RuleFor(c => c.EmpresaId).NotEmpty();
        RuleFor(c => c.Titulo).NotEmpty().MaximumLength(300);
        RuleFor(c => c.TipoEstudio).IsInEnum();
        RuleFor(c => c.PeriodoHasta).GreaterThanOrEqualTo(c => c.PeriodoDesde)
            .WithMessage("El fin del período no puede ser anterior al inicio.");
        RuleFor(c => c.PrimeraVersionObjectKey).NotEmpty().WithMessage("Se requiere el PDF de la primera versión.");
        RuleFor(c => c.PrimeraVersionObjectKey)
            .Must((c, key) => ObjectKeys.PerteneceA(key, c.EmpresaId))
            .When(c => !string.IsNullOrEmpty(c.PrimeraVersionObjectKey))
            .WithMessage("La clave del archivo no pertenece a la empresa indicada.");
    }
}

internal sealed class CrearInformeHandler(
    IInformeRepository repository,
    ITenantContext tenantContext,
    ICurrentUser currentUser,
    IReportingUnitOfWork unitOfWork)
    : ICommandHandler<CrearInformeCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CrearInformeCommand command, CancellationToken cancellationToken)
    {
        tenantContext.SetTenant(command.EmpresaId);

        var periodo = PeriodoCubierto.Create(command.PeriodoDesde, command.PeriodoHasta);
        var informe = Informe.Crear(
            command.EmpresaId, command.Titulo, command.TipoEstudio, periodo,
            command.CampanaId, command.CentroId, currentUser.SubjectId ?? "system", command.PrimeraVersionObjectKey);

        await repository.AddAsync(informe, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(informe.Id);
    }
}
