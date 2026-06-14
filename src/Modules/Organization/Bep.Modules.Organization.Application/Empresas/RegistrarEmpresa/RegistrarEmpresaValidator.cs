using Bep.Modules.Organization.Domain;
using FluentValidation;

namespace Bep.Modules.Organization.Application.Empresas.RegistrarEmpresa;

public sealed class RegistrarEmpresaValidator : AbstractValidator<RegistrarEmpresaCommand>
{
    public RegistrarEmpresaValidator()
    {
        RuleFor(c => c.RazonSocial)
            .NotEmpty().WithMessage("La razón social es obligatoria.")
            .MaximumLength(300);

        RuleFor(c => c.Rut)
            .NotEmpty().WithMessage("El RUT es obligatorio.")
            .Must(BeAValidRut).WithMessage("El RUT no es válido (formato o dígito verificador).");

        RuleFor(c => c.Rubro)
            .MaximumLength(200);
    }

    private static bool BeAValidRut(string rut)
    {
        try
        {
            Rut.Create(rut);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
