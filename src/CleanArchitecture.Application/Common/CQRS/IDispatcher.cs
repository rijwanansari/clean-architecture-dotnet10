namespace CleanArchitecture.Application.Common.CQRS;

/// <summary>
/// Central dispatcher for routing commands and queries to their respective handlers.
/// Commands are run through the validation pipeline before dispatch.
/// </summary>
public interface IDispatcher
{
    Task<TResult> SendAsync<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default);
    Task<TResult> QueryAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default);
}
