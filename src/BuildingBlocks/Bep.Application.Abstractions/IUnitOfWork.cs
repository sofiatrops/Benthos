namespace Bep.Application.Abstractions;

/// <summary>
/// Coordina la persistencia atómica de los cambios de un caso de uso (patrón
/// Unit of Work, SRS 2.7.4). Confirmar guarda los cambios y dispara el despacho
/// de eventos de dominio acumulados.
/// </summary>
public interface IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
