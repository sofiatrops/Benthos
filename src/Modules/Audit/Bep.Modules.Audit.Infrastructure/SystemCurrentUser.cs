using Bep.Application.Abstractions;

namespace Bep.Modules.Audit.Infrastructure;

/// <summary>
/// Actor por defecto para hosts sin contexto HTTP (p. ej. el Worker) o procesos
/// del sistema. Se registra con <c>TryAdd</c>, de modo que el <see cref="ICurrentUser"/>
/// real de la API tiene prioridad cuando existe.
/// </summary>
internal sealed class SystemCurrentUser : ICurrentUser
{
    public bool IsAuthenticated => false;

    public string? SubjectId => "system";

    public PrincipalType PrincipalType => PrincipalType.Anonymous;

    public Guid? TenantId => null;

    public IReadOnlyCollection<string> Roles => [];
}
