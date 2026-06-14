using Bep.Application.Abstractions;
using Bep.Infrastructure.Common.Persistence;
using Bep.Modules.Audit.Infrastructure.DomainEvents;
using Bep.Modules.Audit.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bep.Modules.Audit.Infrastructure;

public static class AuditModuleExtensions
{
    /// <summary>
    /// Registra el módulo de Auditoría (M8): su DbContext y el handler que persiste
    /// los eventos de dominio como registros inmutables.
    /// </summary>
    public static IServiceCollection AddAuditModule(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<AuditDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(AuditDbContext).Assembly.FullName)));

        // Actor por defecto cuando no hay contexto HTTP; la API registra el real antes.
        services.TryAddScoped<ICurrentUser, SystemCurrentUser>();

        services.AddTransient<INotificationHandler<DomainEventNotification>, PersistAuditLogHandler>();

        return services;
    }
}
