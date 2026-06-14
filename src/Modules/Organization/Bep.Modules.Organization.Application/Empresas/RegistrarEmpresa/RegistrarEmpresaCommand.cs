using Bep.Application.Abstractions.Messaging;

namespace Bep.Modules.Organization.Application.Empresas.RegistrarEmpresa;

/// <summary>Registra una nueva empresa (tenant). RF-01-001.</summary>
public sealed record RegistrarEmpresaCommand(string RazonSocial, string Rut, string Rubro) : ICommand<Guid>;
