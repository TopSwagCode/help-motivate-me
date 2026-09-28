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
            await ExecuteAsync(connection, """
                CREATE TABLE IF NOT EXISTS "PushSubscriptions" (
                    "Id" TEXT NOT NULL CONSTRAINT "PK_PushSubscriptions" PRIMARY KEY,
                    "UserId" TEXT NOT NULL,
                    "Endpoint" TEXT NOT NULL,
                    "P256dh" TEXT NOT NULL,
                    "Auth" TEXT NOT NULL,
                    "UserAgent" TEXT NULL,
                    "CreatedAt" TEXT NOT NULL,
                    "LastUsedAt" TEXT NULL,
                    CONSTRAINT "FK_PushSubscriptions_users_UserId" FOREIGN KEY ("UserId") REFERENCES "users" ("Id") ON DELETE CASCADE
                );
                CREATE INDEX IF NOT EXISTS "IX_PushSubscriptions_Endpoint" ON "PushSubscriptions" ("Endpoint");
                CREATE INDEX IF NOT EXISTS "IX_PushSubscriptions_UserId" ON "PushSubscriptions" ("UserId");
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_PushSubscriptions_UserId_Endpoint" ON "PushSubscriptions" ("UserId", "Endpoint");
                """, cancellationToken);

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
