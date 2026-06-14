using Bep.Application.Abstractions;
using Bep.Application.Abstractions.Messaging;
using Bep.Modules.Organization.Application.Abstractions;
using Bep.Modules.Organization.Domain;

namespace Bep.Modules.Organization.Application.Empresas.RegistrarEmpresa;

internal sealed class RegistrarEmpresaHandler(
    IEmpresaRepository empresaRepository,
    IOrganizationUnitOfWork unitOfWork)
    : ICommandHandler<RegistrarEmpresaCommand, Guid>
{
    public async Task<Result<Guid>> Handle(RegistrarEmpresaCommand command, CancellationToken cancellationToken)
    {
        var rut = Rut.Create(command.Rut); // ya validado por el pipeline.

        if (await empresaRepository.ExistsByRutAsync(rut.Value, cancellationToken))
        {
            return Result.Failure<Guid>(Error.Conflict(
                "organization.empresa.rut_duplicado",
                $"Ya existe una empresa con el RUT {rut.Value}."));
        }

        var empresa = Empresa.Registrar(command.RazonSocial, rut, command.Rubro);
        await empresaRepository.AddAsync(empresa, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(empresa.Id);
    }
}
