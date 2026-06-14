namespace Bep.SharedKernel;

/// <summary>
/// Raíz de agregado: única puerta de entrada para modificar el grafo de objetos
/// que protege sus invariantes. Acumula eventos de dominio que la capa de
/// infraestructura publica al guardar (Unit of Work, SRS 2.7.4).
/// </summary>
public abstract class AggregateRoot<TId> : Entity<TId>, IHasDomainEvents
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    protected AggregateRoot(TId id) : base(id) { }

    protected AggregateRoot() { }

    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void RaiseDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}
