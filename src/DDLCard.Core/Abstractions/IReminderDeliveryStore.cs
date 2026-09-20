namespace DDLCard.Core.Abstractions;

public interface IReminderDeliveryStore
{
    Task<bool> WasDeliveredAsync(Guid taskId, DateTimeOffset dueAtUtc, int offsetMinutes, CancellationToken cancellationToken = default);
    Task MarkDeliveredAsync(Guid taskId, DateTimeOffset dueAtUtc, int offsetMinutes, DateTimeOffset deliveredAtUtc, CancellationToken cancellationToken = default);
}

