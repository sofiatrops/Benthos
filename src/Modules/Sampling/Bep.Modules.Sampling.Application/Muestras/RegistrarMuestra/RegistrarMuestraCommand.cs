using Bep.Application.Abstractions;
using Bep.Application.Abstractions.Messaging;
using Bep.Modules.Sampling.Application.Abstractions;
using Bep.Modules.Sampling.Domain;
using FluentValidation;

namespace Bep.Modules.Sampling.Application.Muestras.RegistrarMuestra;

/// <summary>Registra una muestra tomada en terreno (RF-03-001/003/005/009).</summary>
public sealed record RegistrarMuestraCommand(
    Guid EmpresaId,
    Guid CampanaId,
    Guid CentroId,
    TipoMuestra Tipo,
    IReadOnlyList<string> Parametros,
    double Latitud,
    double Longitud,
    double? PrecisionMetros) : ICommand<Guid>;

public sealed class RegistrarMuestraValidator : AbstractValidator<RegistrarMuestraCommand>
{
    public RegistrarMuestraValidator()
    {
        RuleFor(c => c.EmpresaId).NotEmpty();
        RuleFor(c => c.CampanaId).NotEmpty();
        RuleFor(c => c.CentroId).NotEmpty();
        RuleFor(c => c.Tipo).IsInEnum();
        RuleFor(c => c.Latitud).InclusiveBetween(-90, 90);
        RuleFor(c => c.Longitud).InclusiveBetween(-180, 180);
    }
}

internal sealed class RegistrarMuestraHandler(
    IMuestraRepository repository,
    ITenantContext tenantContext,
    ICurrentUser currentUser,
    ISamplingUnitOfWork unitOfWork)
    : ICommandHandler<RegistrarMuestraCommand, Guid>
{
    public async Task<Result<Guid>> Handle(RegistrarMuestraCommand command, CancellationToken cancellationToken)
    {
        tenantContext.SetTenant(command.EmpresaId);

        var ubicacion = UbicacionGps.Create(command.Latitud, command.Longitud, command.PrecisionMetros);
        var muestra = Muestra.Registrar(
            command.EmpresaId, command.CampanaId, command.CentroId, command.Tipo,
            command.Parametros ?? [], ubicacion, currentUser.SubjectId);

        await repository.AddAsync(muestra, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(muestra.Id);
    }
}
