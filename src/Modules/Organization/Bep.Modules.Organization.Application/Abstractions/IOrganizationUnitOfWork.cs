using Bep.Application.Abstractions;

namespace Bep.Modules.Organization.Application.Abstractions;

/// <summary>
/// Unit of Work específico del módulo de Organización (evita ambigüedad de DI
/// cuando conviven varios DbContext).
/// </summary>
public interface IOrganizationUnitOfWork : IUnitOfWork;
