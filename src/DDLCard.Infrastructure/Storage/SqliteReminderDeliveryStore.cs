using System.Globalization;
using DDLCard.Core.Abstractions;
using Microsoft.Data.Sqlite;

namespace DDLCard.Infrastructure.Storage;

public sealed class SqliteReminderDeliveryStore : IReminderDeliveryStore
{
    private readonly string _connectionString;
    private bool _initialized;
    private readonly SemaphoreSlim _initializationLock = new(1, 1);

    public SqliteReminderDeliveryStore(AppDataPaths paths)
    {
        paths.EnsureCreated();
        _connectionString = new SqliteConnectionStringBuilder { DataSource = paths.DatabasePath, Mode = SqliteOpenMode.ReadWriteCreate, Pooling = false }.ToString();
    }

    public async Task<bool> WasDeliveredAsync(Guid taskId, DateTimeOffset dueAtUtc, int offsetMinutes, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1 FROM reminder_deliveries WHERE task_id=$task AND due_at_utc=$due AND offset_minutes=$offset LIMIT 1";
        AddKeyParameters(command, taskId, dueAtUtc, offsetMinutes);
        return await command.ExecuteScalarAsync(cancellationToken) is not null;
    }

    public async Task MarkDeliveredAsync(Guid taskId, DateTimeOffset dueAtUtc, int offsetMinutes, DateTimeOffset deliveredAtUtc, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "INSERT OR IGNORE INTO reminder_deliveries(task_id,due_at_utc,offset_minutes,delivered_at_utc) VALUES($task,$due,$offset,$delivered)";
        AddKeyParameters(command, taskId, dueAtUtc, offsetMinutes);
        command.Parameters.AddWithValue("$delivered", deliveredAtUtc.UtcDateTime.ToString("O", CultureInfo.InvariantCulture));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        if (_initialized) return;
        await _initializationLock.WaitAsync(cancellationToken);
        try
        {
            if (_initialized) return;
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = """
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

    private static void AddKeyParameters(SqliteCommand command, Guid taskId, DateTimeOffset dueAtUtc, int offsetMinutes)
    {
        command.Parameters.AddWithValue("$task", taskId.ToString("D"));
        command.Parameters.AddWithValue("$due", dueAtUtc.UtcDateTime.ToString("O", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$offset", offsetMinutes);
    }
}

