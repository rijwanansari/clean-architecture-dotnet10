namespace CleanArchitecture.Application.Common.CQRS;

public interface ICommand<TResult> { }

/// <summary>Marker interface for commands that produce no meaningful result.</summary>
public interface ICommand : ICommand<Unit> { }
