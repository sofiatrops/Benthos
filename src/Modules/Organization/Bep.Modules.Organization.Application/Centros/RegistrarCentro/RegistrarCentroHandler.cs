using Bep.Application.Abstractions;
using Bep.Application.Abstractions.Messaging;
using Bep.Modules.Organization.Application.Abstractions;
using Bep.Modules.Organization.Domain;

namespace Bep.Modules.Organization.Application.Centros.RegistrarCentro;

internal sealed class RegistrarCentroHandler(
    IEmpresaRepository empresaRepository,
    ITenantContext tenantContext,
    IOrganizationUnitOfWork unitOfWork)
    : ICommandHandler<RegistrarCentroCommand, Guid>
{
    public async Task<Result<Guid>> Handle(RegistrarCentroCommand command, CancellationToken cancellationToken)
    {
        // El centro pertenece al tenant = empresa. Fijar el contexto de tenant
        // habilita la RLS sobre 'centro' para esta operación (ADR-004). Para un
        // usuario cliente, esto debe coincidir con su tenant o se rechaza.
        tenantContext.SetTenant(command.EmpresaId);

        var empresa = await empresaRepository.GetByIdAsync(command.EmpresaId, cancellationToken);
        if (empresa is null)
        {
            return Result.Failure<Guid>(Error.NotFound(
                "organization.empresa.no_encontrada",
                $"No existe la empresa {command.EmpresaId}."));
        }

        if (empresa.Centros.Any(c => string.Equals(c.CodigoInterno, command.CodigoInterno.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            return Result.Failure<Guid>(Error.Conflict(
                "organization.centro.codigo_duplicado",
                $"Ya existe un centro con el código {command.CodigoInterno} en la empresa."));
        }

        var coordenadas = CoordenadasGps.Create(command.Latitud, command.Longitud);
        var centro = empresa.AgregarCentro(command.Nombre, command.CodigoInterno, coordenadas, command.Region);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(centro.Id);
    }
}
