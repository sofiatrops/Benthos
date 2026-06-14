namespace Bep.Modules.Organization.Application.Empresas;

public sealed record EmpresaDto(
    Guid Id,
    string RazonSocial,
    string Rut,
    string Rubro,
    bool Activa,
    DateTimeOffset CreadaUtc);
