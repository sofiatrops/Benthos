using Bep.Application.Abstractions;
using Bep.Application.Abstractions.Messaging;
using Bep.Modules.Insights.Application.Abstractions;
using Bep.Modules.Insights.Application.Generation;
using Bep.Modules.Insights.Domain;
using Bep.Modules.Laboratory.Application.Abstractions;
using FluentValidation;

namespace Bep.Modules.Insights.Application.Analisis.GenerarAnalisis;

/// <summary>
/// Genera un borrador de análisis ambiental para una campaña a partir de sus
/// resultados de laboratorio <b>validados</b> (RF-06-001). El borrador requiere
/// validación profesional antes de ser visible (RF-06-007/010).
/// </summary>
public sealed record GenerarAnalisisCommand(Guid EmpresaId, Guid CampanaId) : ICommand<Guid>;

public sealed class GenerarAnalisisValidator : AbstractValidator<GenerarAnalisisCommand>
{
    public GenerarAnalisisValidator()
    {
        RuleFor(c => c.EmpresaId).NotEmpty();
        RuleFor(c => c.CampanaId).NotEmpty();
    }
}

internal sealed class GenerarAnalisisHandler(
    ITenantContext tenantContext,
    ILaboratoryReadService laboratoryReadService,
    IGeneradorAnalisis generador,
    IAnalisisRepository repository,
    IInsightsUnitOfWork unitOfWork)
    : ICommandHandler<GenerarAnalisisCommand, Guid>
{
    public async Task<Result<Guid>> Handle(GenerarAnalisisCommand command, CancellationToken cancellationToken)
    {
        tenantContext.SetTenant(command.EmpresaId);

        var resultados = await laboratoryReadService.GetResultadosValidadosPorCampanaAsync(
            command.EmpresaId, command.CampanaId, cancellationToken);
        if (resultados.Count == 0)
        {
            return Result.Failure<Guid>(Error.Validation(
                "insights.sin_datos", "La campaña no tiene resultados de laboratorio validados para analizar."));
        }

        // De-identificación por gobierno de datos (ADR-006): solo estadísticas agregadas.
        var parametros = resultados
            .GroupBy(r => (r.Parametro, r.Unidad))
            .Select(g => new ParametroResumen(
                g.Key.Parametro, g.Key.Unidad, g.Count(),
                g.Min(x => x.Valor), g.Max(x => x.Valor), Math.Round(g.Average(x => x.Valor), 4)))
            .ToList();

        var generado = await generador.GenerarAsync(new ContextoAnalisis(command.CampanaId, parametros), cancellationToken);

        var hallazgos = generado.Hallazgos.Select(h => Hallazgo.Crear(h.Parametro, ParseSeveridad(h.Severidad), h.Detalle));
        var analisis = AnalisisAmbiental.Generar(
            command.EmpresaId, command.CampanaId, generado.Resumen, generador.Modelo, hallazgos);

        await repository.AddAsync(analisis, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(analisis.Id);
    }

    private static SeveridadHallazgo ParseSeveridad(string severidad)
        => Enum.TryParse<SeveridadHallazgo>(severidad, ignoreCase: true, out var s) ? s : SeveridadHallazgo.Informativo;
}
