using Bep.Application.Abstractions;
using Bep.Application.Abstractions.Messaging;
using Bep.Application.Abstractions.Storage;
using Bep.Modules.Sampling.Application.Abstractions;
using FluentValidation;

namespace Bep.Modules.Sampling.Application.Muestras.AgregarFoto;

/// <summary>
/// Asocia la referencia de almacenamiento de una fotografía a la muestra (RF-03-004).
/// La subida del archivo al almacenamiento de objetos (URL firmada) se resuelve
/// en la capa de presentación/almacenamiento; aquí solo se registra la referencia.
/// </summary>
public sealed record AgregarFotoCommand(Guid EmpresaId, Guid MuestraId, string ObjectKey) : ICommand;

public sealed class AgregarFotoValidator : AbstractValidator<AgregarFotoCommand>
{
    public AgregarFotoValidator()
    {
        RuleFor(c => c.ObjectKey).NotEmpty()
            .Must((c, key) => ObjectKeys.PerteneceA(key, c.EmpresaId))
            .WithMessage("La clave de la foto no pertenece a la empresa indicada.");
    }
}

internal sealed class AgregarFotoHandler(
    IMuestraRepository repository,
    ITenantContext tenantContext,
    ICurrentUser currentUser,
    ISamplingUnitOfWork unitOfWork)
    : ICommandHandler<AgregarFotoCommand>
{
    public async Task<Result> Handle(AgregarFotoCommand command, CancellationToken cancellationToken)
    {
        tenantContext.SetTenant(command.EmpresaId);

        var muestra = await repository.GetByIdAsync(command.MuestraId, cancellationToken);
        if (muestra is null)
        {
            return Result.Failure(Error.NotFound("sampling.muestra.no_encontrada", $"No existe la muestra {command.MuestraId}."));
        }

        muestra.AgregarFoto(command.ObjectKey, currentUser.SubjectId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
