using System.Reflection;
using Bep.Application.Abstractions;
using Bep.SharedKernel;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Bep.Infrastructure.Common.Persistence;

/// <summary>
/// DbContext base de todos los módulos. Aporta dos garantías transversales:
/// <list type="number">
///   <item>Filtrado por tenant en capa de aplicación para toda entidad
///   <see cref="ITenantScoped"/> (primera capa de aislamiento; la segunda es RLS).</item>
///   <item>Despacho de eventos de dominio acumulados en los agregados tras
///   persistir (patrón Observer hacia Auditoría/Notificaciones).</item>
/// </list>
/// </summary>
public abstract class BepDbContextBase(
    DbContextOptions options,
    ITenantContext tenantContext,
    IPublisher? publisher = null) : DbContext(options)
{
    /// <summary>
    /// Aplica un filtro global <c>tenant_id == tenant efectivo</c> a cada entidad
    /// tenant-scoped. Llamar desde <see cref="DbContext.OnModelCreating"/> del módulo.
    /// </summary>
    protected void ApplyTenantQueryFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ITenantScoped).IsAssignableFrom(entityType.ClrType))
            {
                typeof(BepDbContextBase)
                    .GetMethod(nameof(ConfigureTenantFilter), BindingFlags.Instance | BindingFlags.NonPublic)!
                    .MakeGenericMethod(entityType.ClrType)
                    .Invoke(this, [modelBuilder]);
            }
        }
    }

    private void ConfigureTenantFilter<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class, ITenantScoped
        => modelBuilder.Entity<TEntity>().HasQueryFilter(e => e.TenantId == tenantContext.TenantId);

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var domainEvents = ExtractDomainEvents();
        var result = await base.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // Nota: para garantía transaccional estricta, evolucionar a un patrón
        // Outbox. Para la Fase 0 el despacho in-process es suficiente.
        if (publisher is not null)
        {
            foreach (var domainEvent in domainEvents)
            {
                await publisher.Publish(domainEvent, cancellationToken).ConfigureAwait(false);
            }
        }

        return result;
    }

    private List<INotification> ExtractDomainEvents()
    {
        var aggregates = ChangeTracker
            .Entries()
            .Where(e => e.Entity is IHasDomainEvents)
            .Select(e => (IHasDomainEvents)e.Entity)
            .Where(a => a.DomainEvents.Count > 0)
            .ToList();

        var events = aggregates
            .SelectMany(a => a.DomainEvents)
            .Select(e => new DomainEventNotification(e))
            .Cast<INotification>()
            .ToList();

        foreach (var aggregate in aggregates)
        {
            aggregate.ClearDomainEvents();
        }

        return events;
    }
}

/// <summary>Envoltura MediatR para publicar un <see cref="IDomainEvent"/> sin acoplar el dominio a MediatR.</summary>
public sealed record DomainEventNotification(IDomainEvent DomainEvent) : INotification;
