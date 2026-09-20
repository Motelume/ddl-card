using System.Globalization;
using System.Text.Json;
using DDLCard.Core.Abstractions;
using DDLCard.Core.Models;
using DDLCard.Core.Services;
using DDLCard.Infrastructure.Serialization;
using Microsoft.Data.Sqlite;

namespace DDLCard.Infrastructure.Storage;

public sealed class SqliteTaskRepository : ITaskRepository
{
    private readonly string _connectionString;
    private readonly JsonSerializerOptions _jsonOptions = JsonDefaults.Create();
    private readonly SemaphoreSlim _initializationLock = new(1, 1);
    private bool _initialized;

    public SqliteTaskRepository(AppDataPaths paths)
    {
        paths.EnsureCreated();
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = paths.DatabasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared,
            Pooling = false
        }.ToString();
    }

    public async Task<IReadOnlyList<DeadlineTask>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT payload_json FROM tasks ORDER BY due_at_utc, priority DESC, created_at_utc";

        var results = new List<DeadlineTask>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(Deserialize(reader.GetString(0)));
        }

        return results;
    }

    public async Task<DeadlineTask?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT payload_json FROM tasks WHERE id = $id";
        command.Parameters.AddWithValue("$id", id.ToString("D"));
        var json = await command.ExecuteScalarAsync(cancellationToken) as string;
        return json is null ? null : Deserialize(json);
    }

    public async Task UpsertAsync(DeadlineTask task, CancellationToken cancellationToken = default)
    {
        TaskValidator.Validate(task);
        task.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await EnsureInitializedAsync(cancellationToken);
        await using var connection = await OpenAsync(cancellationToken);
        await UpsertInternalAsync(connection, null, task, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM tasks WHERE id = $id";
        command.Parameters.AddWithValue("$id", id.ToString("D"));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task ReplaceAllAsync(IEnumerable<DeadlineTask> tasks, CancellationToken cancellationToken = default)
    {
        var materialized = tasks.ToList();
        foreach (var task in materialized)
        {
            TaskValidator.Validate(task);
        }

        await EnsureInitializedAsync(cancellationToken);
        await using var connection = await OpenAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);
        await using (var delete = connection.CreateCommand())
        {
            delete.Transaction = transaction;
            delete.CommandText = "DELETE FROM tasks";
            await delete.ExecuteNonQueryAsync(cancellationToken);
        }

        foreach (var task in materialized)
        {
            await UpsertInternalAsync(connection, transaction, task, cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    private async Task EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        if (_initialized)
        {
            return;
        }

        await _initializationLock.WaitAsync(cancellationToken);
        try
        {
            if (_initialized)
            {
                return;
            }

            await using var connection = await OpenAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = """
                PRAGMA journal_mode = WAL;
                CREATE TABLE IF NOT EXISTS tasks (
                    id TEXT PRIMARY KEY,
                    due_at_utc TEXT NOT NULL,
                    priority INTEGER NOT NULL,
                    state INTEGER NOT NULL,
                    created_at_utc TEXT NOT NULL,
                    payload_json TEXT NOT NULL
                );
                CREATE INDEX IF NOT EXISTS ix_tasks_due ON tasks(state, due_at_utc, priority DESC);
                CREATE TABLE IF NOT EXISTS reminder_deliveries (
                    task_id TEXT NOT NULL,
                    due_at_utc TEXT NOT NULL,
                    offset_minutes INTEGER NOT NULL,
                    delivered_at_utc TEXT NOT NULL,
                    PRIMARY KEY(task_id, due_at_utc, offset_minutes)
                );
                """;
            await command.ExecuteNonQueryAsync(cancellationToken);
            _initialized = true;
        }
        finally
        {
            _initializationLock.Release();
        }
    }

    private async Task<SqliteConnection> OpenAsync(CancellationToken cancellationToken)
    {
        var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    private async Task UpsertInternalAsync(
        SqliteConnection connection,
        SqliteTransaction? transaction,
        DeadlineTask task,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO tasks(id, due_at_utc, priority, state, created_at_utc, payload_json)
            VALUES($id, $due, $priority, $state, $created, $payload)
            ON CONFLICT(id) DO UPDATE SET
                due_at_utc = excluded.due_at_utc,
                priority = excluded.priority,
                state = excluded.state,
                created_at_utc = excluded.created_at_utc,
                payload_json = excluded.payload_json;
            """;
        command.Parameters.AddWithValue("$id", task.Id.ToString("D"));
        command.Parameters.AddWithValue("$due", task.DueAtUtc.UtcDateTime.ToString("O", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$priority", (int)task.Priority);
        command.Parameters.AddWithValue("$state", (int)task.State);
        command.Parameters.AddWithValue("$created", task.CreatedAtUtc.UtcDateTime.ToString("O", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$payload", JsonSerializer.Serialize(task, _jsonOptions));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private DeadlineTask Deserialize(string json) =>
        JsonSerializer.Deserialize<DeadlineTask>(json, _jsonOptions)
        ?? throw new InvalidDataException("数据库中的任务记录无法读取。");
}
