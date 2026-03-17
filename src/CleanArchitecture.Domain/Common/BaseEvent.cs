namespace CleanArchitecture.Domain.Common;

public abstract record BaseEvent
{
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}
