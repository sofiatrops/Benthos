using FluentValidation;

namespace Bep.Modules.Organization.Application.Centros.RegistrarCentro;

public sealed class RegistrarCentroValidator : AbstractValidator<RegistrarCentroCommand>
{
    public RegistrarCentroValidator()
    {
        RuleFor(c => c.EmpresaId).NotEmpty();
        RuleFor(c => c.Nombre).NotEmpty().MaximumLength(300);
        RuleFor(c => c.CodigoInterno).NotEmpty().MaximumLength(100);
        RuleFor(c => c.Latitud).InclusiveBetween(-90, 90);
        RuleFor(c => c.Longitud).InclusiveBetween(-180, 180);
        RuleFor(c => c.Region).MaximumLength(150);
    }
}
