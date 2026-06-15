using Bep.Application.Abstractions;
using Bep.Application.Abstractions.Messaging;
using Bep.Application.Abstractions.Storage;
using Bep.Modules.Laboratory.Application.Abstractions;
using Bep.Modules.Laboratory.Application.Parsing;
using Bep.Modules.Laboratory.Domain;
using FluentValidation;

namespace Bep.Modules.Laboratory.Application.LoteResultados.ImportarResultados;

/// <summary>
/// Ingesta de un lote de resultados desde un archivo estructurado ya subido al
/// almacén (RF-04-001). El binario no atraviesa la API: se referencia por su
/// <c>ObjectKey</c> (ADR-008) y el servidor lo lee y parsea.
/// </summary>
public sealed record ImportarResultadosCommand(
    Guid EmpresaId,
    Guid CampanaId,
    string Laboratorio,
    string ObjectKey,
    string Formato = "csv") : ICommand<ImportarResultadosResult>;

/// <summary>Resultado de la ingesta: lote creado, filas importadas y errores de parseo no fatales.</summary>
public sealed record ImportarResultadosResult(Guid LoteId, int Importados, IReadOnlyList<string> Errores);

public sealed class ImportarResultadosValidator : AbstractValidator<ImportarResultadosCommand>
{
    public ImportarResultadosValidator()
    {
        RuleFor(c => c.EmpresaId).NotEmpty();
        RuleFor(c => c.CampanaId).NotEmpty();
        RuleFor(c => c.Laboratorio).NotEmpty().MaximumLength(200);
        RuleFor(c => c.ObjectKey).NotEmpty()
            .Must((c, key) => ObjectKeys.PerteneceA(key, c.EmpresaId))
            .WithMessage("La clave del archivo no pertenece a la empresa indicada.");
        RuleFor(c => c.Formato).NotEmpty();
    }
}

internal sealed class ImportarResultadosHandler(
    ITenantContext tenantContext,
    IObjectStorage objectStorage,
    IEnumerable<IResultadosParser> parsers,
    ILoteResultadosRepository repository,
    ILaboratoryUnitOfWork unitOfWork)
    : ICommandHandler<ImportarResultadosCommand, ImportarResultadosResult>
{
    public async Task<Result<ImportarResultadosResult>> Handle(
        ImportarResultadosCommand command, CancellationToken cancellationToken)
    {
        tenantContext.SetTenant(command.EmpresaId);

        var parser = parsers.FirstOrDefault(p => p.Formato.Equals(command.Formato, StringComparison.OrdinalIgnoreCase));
        if (parser is null)
        {
            return Result.Failure<ImportarResultadosResult>(Error.Validation(
                "laboratory.formato_no_soportado", $"No hay un lector para el formato '{command.Formato}'."));
        }

        var archivo = await objectStorage.AbrirLecturaAsync(command.ObjectKey, cancellationToken);
        if (archivo.IsFailure)
        {
            return Result.Failure<ImportarResultadosResult>(archivo.Error!);
        }

        ParseResultado parseado;
        await using (var stream = archivo.Value)
        {
            parseado = parser.Parse(stream);
        }

        if (parseado.Resultados.Count == 0)
        {
            return Result.Failure<ImportarResultadosResult>(Error.Validation(
                "laboratory.sin_resultados", "El archivo no contiene resultados válidos para importar."));
        }

        var resultados = parseado.Resultados
            .Select(d => ResultadoParametro.Crear(d.CodigoMuestra, d.Parametro, d.Valor, d.Unidad, d.Metodo))
            .ToList();

        var lote = Domain.LoteResultados.Recibir(
            command.EmpresaId, command.CampanaId, command.Laboratorio, command.ObjectKey, resultados);

        await repository.AddAsync(lote, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new ImportarResultadosResult(lote.Id, resultados.Count, parseado.Errores));
    }
}
