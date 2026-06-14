namespace Bep.SharedKernel;

/// <summary>
/// Expone los eventos de dominio acumulados por un agregado, para que la capa de
/// infraestructura los despache al persistir. Definido en el dominio para no
/// acoplar los agregados a la infraestructura ni a MediatR.
/// </summary>
public interface IHasDomainEvents
{
    public IReadOnlyList<IDomainEvent> DomainEvents { get; }

    public void ClearDomainEvents();
}
