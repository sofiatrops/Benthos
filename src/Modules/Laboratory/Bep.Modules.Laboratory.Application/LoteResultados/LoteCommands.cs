using Bep.Application.Abstractions;
using Bep.Application.Abstractions.Messaging;
using Bep.Modules.Laboratory.Application.Abstractions;

namespace Bep.Modules.Laboratory.Application.LoteResultados;

/// <summary>Valida un lote recibido: sus parámetros pasan a alimentar indicadores (RF-04-005).</summary>
public sealed record ValidarLoteCommand(Guid EmpresaId, Guid LoteId) : ICommand;

/// <summary>Rechaza un lote por inconsistencias, con un motivo.</summary>
public sealed record RechazarLoteCommand(Guid EmpresaId, Guid LoteId, string Motivo) : ICommand;

internal abstract class LoteCommandHandlerBase(
    ITenantContext tenantContext,
    ILoteResultadosRepository repository,
    ILaboratoryUnitOfWork unitOfWork)
{
    protected async Task<Result<Domain.LoteResultados>> CargarAsync(Guid empresaId, Guid loteId, CancellationToken ct)
    {
        tenantContext.SetTenant(empresaId);
        var lote = await repository.GetByIdAsync(loteId, ct);
        return lote is null
            ? Result.Failure<Domain.LoteResultados>(Error.NotFound("laboratory.lote.no_encontrado", $"No existe el lote {loteId}."))
            : Result.Success(lote);
    }

    protected Task<int> GuardarAsync(CancellationToken ct) => unitOfWork.SaveChangesAsync(ct);
}

internal sealed class ValidarLoteHandler(
    ITenantContext tenantContext, ILoteResultadosRepository repository, ILaboratoryUnitOfWork unitOfWork)
    : LoteCommandHandlerBase(tenantContext, repository, unitOfWork), ICommandHandler<ValidarLoteCommand>
{
    public async Task<Result> Handle(ValidarLoteCommand command, CancellationToken cancellationToken)
    {
        var cargado = await CargarAsync(command.EmpresaId, command.LoteId, cancellationToken);
        if (cargado.IsFailure)
        {
            return Result.Failure(cargado.Error!);
        }

        try
        {
            cargado.Value.Validar();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(Error.Conflict("laboratory.lote.estado_invalido", ex.Message));
        }

        await GuardarAsync(cancellationToken);
        return Result.Success();
    }
}

internal sealed class RechazarLoteHandler(
    ITenantContext tenantContext, ILoteResultadosRepository repository, ILaboratoryUnitOfWork unitOfWork)
    : LoteCommandHandlerBase(tenantContext, repository, unitOfWork), ICommandHandler<RechazarLoteCommand>
{
    public async Task<Result> Handle(RechazarLoteCommand command, CancellationToken cancellationToken)
    {
        var cargado = await CargarAsync(command.EmpresaId, command.LoteId, cancellationToken);
        if (cargado.IsFailure)
        {
            return Result.Failure(cargado.Error!);
        }

        try
        {
            cargado.Value.Rechazar(command.Motivo);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(Error.Conflict("laboratory.lote.estado_invalido", ex.Message));
        }
        catch (ArgumentException ex)
        {
            return Result.Failure(Error.Validation("laboratory.lote.motivo_requerido", ex.Message));
        }

        await GuardarAsync(cancellationToken);
        return Result.Success();
    }
}
