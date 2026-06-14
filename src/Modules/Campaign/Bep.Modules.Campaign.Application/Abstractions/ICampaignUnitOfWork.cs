using Bep.Application.Abstractions;

namespace Bep.Modules.Campaign.Application.Abstractions;

/// <summary>
/// Unit of Work específico del módulo de Campañas. Cada módulo tiene el suyo para
/// evitar ambigüedad de resolución cuando conviven varios DbContext en el mismo
/// contenedor de DI.
/// </summary>
public interface ICampaignUnitOfWork : IUnitOfWork;
