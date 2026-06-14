using Bep.Application.Abstractions;
using Bep.Application.Abstractions.Messaging;
using Bep.Modules.Sampling.Application.Abstractions;
using Bep.Modules.Sampling.Domain;

namespace Bep.Modules.Sampling.Application.Muestras.Custodia;

/// <summary>Transfiere la custodia de una muestra a otro responsable (RF-03-007).</summary>
public sealed record TransferirCustodiaCommand(Guid EmpresaId, Guid MuestraId, string ParaSubjectId) : ICommand;

/// <summary>Acepta la custodia pendiente; la muestra pasa a recibida en laboratorio (RF-03-007).</summary>
public sealed record AceptarCustodiaCommand(Guid EmpresaId, Guid MuestraId) : ICommand;

internal sealed class TransferirCustodiaHandler(
    IMuestraRepository repository,
    ITenantContext tenantContext,
    ICurrentUser currentUser,
    ISamplingUnitOfWork unitOfWork)
    : ICommandHandler<TransferirCustodiaCommand>
{
    public async Task<Result> Handle(TransferirCustodiaCommand command, CancellationToken cancellationToken)
    {
        tenantContext.SetTenant(command.EmpresaId);

        var muestra = await repository.GetByIdAsync(command.MuestraId, cancellationToken);
        if (muestra is null)
        {
            return Result.Failure(Error.NotFound("sampling.muestra.no_encontrada", $"No existe la muestra {command.MuestraId}."));
        }

        if (muestra.Estado == EstadoMuestra.Archivada)
        {
            return Result.Failure(Error.Conflict(
                "sampling.custodia.muestra_archivada", "No se puede transferir la custodia de una muestra archivada."));
        }

        muestra.TransferirCustodia(currentUser.SubjectId, command.ParaSubjectId, currentUser.SubjectId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

internal sealed class AceptarCustodiaHandler(
    IMuestraRepository repository,
    ITenantContext tenantContext,
    ICurrentUser currentUser,
    ISamplingUnitOfWork unitOfWork)
    : ICommandHandler<AceptarCustodiaCommand>
{
    public async Task<Result> Handle(AceptarCustodiaCommand command, CancellationToken cancellationToken)
    {
        tenantContext.SetTenant(command.EmpresaId);

        var muestra = await repository.GetByIdAsync(command.MuestraId, cancellationToken);
        if (muestra is null)
        {
            return Result.Failure(Error.NotFound("sampling.muestra.no_encontrada", $"No existe la muestra {command.MuestraId}."));
        }

        if (!muestra.Custodias.Any(c => !c.Aceptada))
        {
            return Result.Failure(Error.Conflict(
                "sampling.custodia.sin_pendiente", "No hay una transferencia de custodia pendiente de aceptar."));
        }

        muestra.AceptarCustodia(currentUser.SubjectId ?? "system");
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
