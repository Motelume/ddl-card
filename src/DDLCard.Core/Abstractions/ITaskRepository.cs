using DDLCard.Core.Models;

namespace DDLCard.Core.Abstractions;

public interface ITaskRepository
{
    Task<IReadOnlyList<DeadlineTask>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<DeadlineTask?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task UpsertAsync(DeadlineTask task, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task ReplaceAllAsync(IEnumerable<DeadlineTask> tasks, CancellationToken cancellationToken = default);
}

