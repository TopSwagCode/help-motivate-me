using System.Data;
using HelpMotivateMe.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HelpMotivateMe.Infrastructure.Services;

public static class SqliteSchemaUpdater
{
    public static async Task UpgradeAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        var connection = db.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose) await connection.OpenAsync(cancellationToken);

        try
        {
            var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            await using (var command = connection.CreateCommand())
            {
                command.CommandText = "PRAGMA table_info('habit_stacks');";
                await using var reader = await command.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken)) columns.Add(reader.GetString(1));
            }

            if (!columns.Contains("OddWeekDays"))
                await ExecuteAsync(connection, "ALTER TABLE habit_stacks ADD COLUMN OddWeekDays INTEGER NOT NULL DEFAULT 127;", cancellationToken);

            if (!columns.Contains("EvenWeekDays"))
                await ExecuteAsync(connection, "ALTER TABLE habit_stacks ADD COLUMN EvenWeekDays INTEGER NOT NULL DEFAULT 127;", cancellationToken);
        }
        finally
        {
            if (shouldClose) await connection.CloseAsync();
        }
    }

    private static async Task ExecuteAsync(
        System.Data.Common.DbConnection connection,
        string commandText,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = commandText;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
