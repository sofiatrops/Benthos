using Bep.Application.Abstractions;
using Bep.Application.Abstractions.Messaging;
using Bep.Modules.Insights.Application.Abstractions;

namespace Bep.Modules.Insights.Application.Analisis;

/// <summary>Validación profesional de un borrador de análisis (RF-06-007).</summary>
public sealed record ValidarAnalisisCommand(Guid EmpresaId, Guid AnalisisId) : ICommand;

/// <summary>Descarta un borrador de análisis, con motivo.</summary>
public sealed record DescartarAnalisisCommand(Guid EmpresaId, Guid AnalisisId, string Motivo) : ICommand;

internal abstract class AnalisisCommandHandlerBase(
    ITenantContext tenantContext,
    IAnalisisRepository repository,
    IInsightsUnitOfWork unitOfWork)
{
    protected async Task<Result<Domain.AnalisisAmbiental>> CargarAsync(Guid empresaId, Guid analisisId, CancellationToken ct)
    {
        tenantContext.SetTenant(empresaId);
        var analisis = await repository.GetByIdAsync(analisisId, ct);
        return analisis is null
            ? Result.Failure<Domain.AnalisisAmbiental>(Error.NotFound("insights.analisis.no_encontrado", $"No existe el análisis {analisisId}."))
            : Result.Success(analisis);
    }

    protected Task<int> GuardarAsync(CancellationToken ct) => unitOfWork.SaveChangesAsync(ct);
}

internal sealed class ValidarAnalisisHandler(
    ITenantContext tenantContext, IAnalisisRepository repository, ICurrentUser currentUser, IInsightsUnitOfWork unitOfWork)
    : AnalisisCommandHandlerBase(tenantContext, repository, unitOfWork), ICommandHandler<ValidarAnalisisCommand>
{
    public async Task<Result> Handle(ValidarAnalisisCommand command, CancellationToken cancellationToken)
    {
        var cargado = await CargarAsync(command.EmpresaId, command.AnalisisId, cancellationToken);
        if (cargado.IsFailure)
        {
            return Result.Failure(cargado.Error!);
        }

        try
        {
            cargado.Value.Validar(currentUser.SubjectId ?? "system");
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(Error.Conflict("insights.analisis.estado_invalido", ex.Message));
        }

        await GuardarAsync(cancellationToken);
        return Result.Success();
    }
}

internal sealed class DescartarAnalisisHandler(
    ITenantContext tenantContext, IAnalisisRepository repository, IInsightsUnitOfWork unitOfWork)
    : AnalisisCommandHandlerBase(tenantContext, repository, unitOfWork), ICommandHandler<DescartarAnalisisCommand>
{
    public async Task<Result> Handle(DescartarAnalisisCommand command, CancellationToken cancellationToken)
    {
        var cargado = await CargarAsync(command.EmpresaId, command.AnalisisId, cancellationToken);
        if (cargado.IsFailure)
        {
            return Result.Failure(cargado.Error!);
        }

        try
        {
            cargado.Value.Descartar(command.Motivo);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(Error.Conflict("insights.analisis.estado_invalido", ex.Message));
        }
        catch (ArgumentException ex)
        {
            return Result.Failure(Error.Validation("insights.analisis.motivo_requerido", ex.Message));
        }

        await GuardarAsync(cancellationToken);
        return Result.Success();
    }
}
