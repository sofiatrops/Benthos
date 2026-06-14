using Bep.Application.Abstractions;
using Bep.Application.Abstractions.Messaging;
using Bep.Modules.Sampling.Application.Abstractions;
using Bep.Modules.Sampling.Domain;

namespace Bep.Modules.Sampling.Application.Muestras.TransicionarEstado;

/// <summary>Avanza el estado de laboratorio de una muestra (análisis, resultado, archivo).</summary>
public sealed record TransicionarEstadoMuestraCommand(
    Guid EmpresaId, Guid MuestraId, EstadoMuestra NuevoEstado, string? Descripcion) : ICommand;

internal sealed class TransicionarEstadoMuestraHandler(
    IMuestraRepository repository,
    ITenantContext tenantContext,
    ICurrentUser currentUser,
    ISamplingUnitOfWork unitOfWork)
    : ICommandHandler<TransicionarEstadoMuestraCommand>
{
    public async Task<Result> Handle(TransicionarEstadoMuestraCommand command, CancellationToken cancellationToken)
    {
        tenantContext.SetTenant(command.EmpresaId);

        var muestra = await repository.GetByIdAsync(command.MuestraId, cancellationToken);
        if (muestra is null)
        {
            return Result.Failure(Error.NotFound("sampling.muestra.no_encontrada", $"No existe la muestra {command.MuestraId}."));
        }

        if (!muestra.PuedeTransicionarA(command.NuevoEstado))
        {
            return Result.Failure(Error.Conflict(
                "sampling.muestra.transicion_invalida",
                $"Transición no permitida: {muestra.Estado} → {command.NuevoEstado}."));
        }

        muestra.Transicionar(command.NuevoEstado, currentUser.SubjectId, command.Descripcion ?? command.NuevoEstado.ToString());
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
