using Mediator;

namespace MemoAna.Application.Common.Abstractions;

/// <summary>
/// Marks a message as requiring a transaction.
/// </summary>
public interface ITransactionalRequest : IMessage
{
}

/// <summary>
/// Marks a request as requiring a transaction.
/// </summary>
/// <typeparam name="TResponse">The response type.</typeparam>
public interface ITransactionalRequest<TResponse>
    : ITransactionalRequest,
      IRequest<TResponse>
{
}
