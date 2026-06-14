using Bep.Application.Abstractions;
using Bep.Application.Abstractions.Messaging;
using Bep.Modules.Reporting.Application.Abstractions;
using Bep.Modules.Reporting.Domain;

namespace Bep.Modules.Reporting.Application.Informes;

/// <summary>Carga una nueva versión del PDF del informe (RF-05-002).</summary>
public sealed record CargarVersionCommand(Guid EmpresaId, Guid InformeId, string ObjectKey) : ICommand;

/// <summary>Agrega un comentario interno de revisión, no visible para el cliente (RF-05-004).</summary>
public sealed record AgregarComentarioCommand(Guid EmpresaId, Guid InformeId, string Texto) : ICommand;

/// <summary>Anexa un documento complementario al informe (RF-05-009).</summary>
public sealed record AgregarAnexoCommand(Guid EmpresaId, Guid InformeId, string ObjectKey, string Descripcion) : ICommand;

/// <summary>Avanza el flujo de revisión/publicación de un informe (RF-05-003).</summary>
public sealed record TransicionarEstadoInformeCommand(Guid EmpresaId, Guid InformeId, EstadoInforme NuevoEstado) : ICommand;

/// <summary>Archiva (eliminación lógica) un informe; restringido a roles administrativos (RF-05-010).</summary>
public sealed record ArchivarInformeCommand(Guid EmpresaId, Guid InformeId) : ICommand;

internal abstract class InformeCommandHandlerBase(
    IInformeRepository repository,
    ITenantContext tenantContext,
    IReportingUnitOfWork unitOfWork)
{
    protected async Task<Result<Informe>> CargarAsync(Guid empresaId, Guid informeId, CancellationToken ct)
    {
        tenantContext.SetTenant(empresaId);
        var informe = await repository.GetByIdAsync(informeId, ct);
        return informe is null
            ? Result.Failure<Informe>(Error.NotFound("reporting.informe.no_encontrado", $"No existe el informe {informeId}."))
            : Result.Success(informe);
    }

    protected Task<int> GuardarAsync(CancellationToken ct) => unitOfWork.SaveChangesAsync(ct);
}

internal sealed class CargarVersionHandler(
    IInformeRepository repository, ITenantContext tenantContext, ICurrentUser currentUser, IReportingUnitOfWork unitOfWork)
    : InformeCommandHandlerBase(repository, tenantContext, unitOfWork), ICommandHandler<CargarVersionCommand>
{
    public async Task<Result> Handle(CargarVersionCommand command, CancellationToken cancellationToken)
    {
        var cargado = await CargarAsync(command.EmpresaId, command.InformeId, cancellationToken);
        if (cargado.IsFailure)
        {
            return Result.Failure(cargado.Error!);
        }

        var informe = cargado.Value;
        if (informe.Estado is EstadoInforme.Publicado or EstadoInforme.Archivado)
        {
            return Result.Failure(Error.Conflict(
                "reporting.informe.no_editable", "No se pueden cargar versiones de un informe publicado o archivado."));
        }

        informe.CargarVersion(command.ObjectKey, currentUser.SubjectId);
        await GuardarAsync(cancellationToken);
        return Result.Success();
    }
}

internal sealed class AgregarComentarioHandler(
    IInformeRepository repository, ITenantContext tenantContext, ICurrentUser currentUser, IReportingUnitOfWork unitOfWork)
    : InformeCommandHandlerBase(repository, tenantContext, unitOfWork), ICommandHandler<AgregarComentarioCommand>
{
    public async Task<Result> Handle(AgregarComentarioCommand command, CancellationToken cancellationToken)
    {
        var cargado = await CargarAsync(command.EmpresaId, command.InformeId, cancellationToken);
        if (cargado.IsFailure)
        {
            return Result.Failure(cargado.Error!);
        }

        cargado.Value.AgregarComentarioInterno(currentUser.SubjectId ?? "system", command.Texto);
        await GuardarAsync(cancellationToken);
        return Result.Success();
    }
}

internal sealed class AgregarAnexoHandler(
    IInformeRepository repository, ITenantContext tenantContext, IReportingUnitOfWork unitOfWork)
    : InformeCommandHandlerBase(repository, tenantContext, unitOfWork), ICommandHandler<AgregarAnexoCommand>
{
    public async Task<Result> Handle(AgregarAnexoCommand command, CancellationToken cancellationToken)
    {
        var cargado = await CargarAsync(command.EmpresaId, command.InformeId, cancellationToken);
        if (cargado.IsFailure)
        {
            return Result.Failure(cargado.Error!);
        }

        cargado.Value.AgregarAnexo(command.ObjectKey, command.Descripcion);
        await GuardarAsync(cancellationToken);
        return Result.Success();
    }
}

internal sealed class TransicionarEstadoInformeHandler(
    IInformeRepository repository, ITenantContext tenantContext, IReportingUnitOfWork unitOfWork)
    : InformeCommandHandlerBase(repository, tenantContext, unitOfWork), ICommandHandler<TransicionarEstadoInformeCommand>
{
    public async Task<Result> Handle(TransicionarEstadoInformeCommand command, CancellationToken cancellationToken)
    {
        if (command.NuevoEstado == EstadoInforme.Archivado)
        {
            return Result.Failure(Error.Conflict(
                "reporting.informe.usar_archivar", "El archivado se realiza por la operación específica (rol administrativo)."));
        }

        var cargado = await CargarAsync(command.EmpresaId, command.InformeId, cancellationToken);
        if (cargado.IsFailure)
        {
            return Result.Failure(cargado.Error!);
        }

        var informe = cargado.Value;
        if (!informe.PuedeTransicionarA(command.NuevoEstado))
        {
            return Result.Failure(Error.Conflict(
                "reporting.informe.transicion_invalida", $"Transición no permitida: {informe.Estado} → {command.NuevoEstado}."));
        }

        informe.Transicionar(command.NuevoEstado);
        await GuardarAsync(cancellationToken);
        return Result.Success();
    }
}

internal sealed class ArchivarInformeHandler(
    IInformeRepository repository, ITenantContext tenantContext, IReportingUnitOfWork unitOfWork)
    : InformeCommandHandlerBase(repository, tenantContext, unitOfWork), ICommandHandler<ArchivarInformeCommand>
{
    public async Task<Result> Handle(ArchivarInformeCommand command, CancellationToken cancellationToken)
    {
        var cargado = await CargarAsync(command.EmpresaId, command.InformeId, cancellationToken);
        if (cargado.IsFailure)
        {
            return Result.Failure(cargado.Error!);
        }

        var informe = cargado.Value;
        if (informe.Estado == EstadoInforme.Archivado)
        {
            return Result.Failure(Error.Conflict("reporting.informe.ya_archivado", "El informe ya está archivado."));
        }

        informe.Archivar();
        await GuardarAsync(cancellationToken);
        return Result.Success();
    }
}
