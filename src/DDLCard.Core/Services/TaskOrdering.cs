using DDLCard.Core.Models;

namespace DDLCard.Core.Services;

public static class TaskOrdering
{
    public static IReadOnlyList<DeadlineTask> ForCard(IEnumerable<DeadlineTask> tasks) =>
        tasks.Where(x => x.State == TaskState.Active)
            .OrderBy(x => x.DueAtUtc)
            .ThenByDescending(x => x.Priority)
            .ThenBy(x => x.CreatedAtUtc)
            .ToList();
}

