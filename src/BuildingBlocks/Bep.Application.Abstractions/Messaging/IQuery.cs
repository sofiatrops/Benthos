using MediatR;

namespace Bep.Application.Abstractions.Messaging;

/// <summary>Consulta de solo lectura que devuelve un valor (CQRS, lado de lectura).</summary>
public interface IQuery<TResponse> : IRequest<Result<TResponse>>;

public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse>;
