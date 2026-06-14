namespace Bep.SharedKernel;

/// <summary>
/// Marca una entidad como perteneciente a un tenant (empresa cliente). Toda
/// entidad con esta marca queda sujeta al filtrado por <c>TenantId</c> en la
/// capa de aplicación y a Row-Level Security en PostgreSQL (ADR-004, RNF-SEG-008).
/// </summary>
public interface ITenantScoped
{
    public Guid TenantId { get; }
}
