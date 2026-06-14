using MediatR;

namespace Bep.Application.Abstractions.Messaging;

/// <summary>Comando que modifica estado y no devuelve valor (CQRS, lado de escritura).</summary>
public interface ICommand : IRequest<Result>;

/// <summary>Comando que modifica estado y devuelve un valor.</summary>
public interface ICommand<TResponse> : IRequest<Result<TResponse>>;

public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand, Result>
    where TCommand : ICommand;

public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, Result<TResponse>>
    where TCommand : ICommand<TResponse>;
