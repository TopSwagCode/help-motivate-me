using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace HelpMotivateMe.Infrastructure.Services;

public sealed class SqliteConnectionInterceptor : DbConnectionInterceptor
{
    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        Initialize(connection);
    }

    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        RegisterFunctions(connection);
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA foreign_keys=ON; PRAGMA busy_timeout=30000; PRAGMA journal_mode=WAL;";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void Initialize(DbConnection connection)
    {
        RegisterFunctions(connection);
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA foreign_keys=ON; PRAGMA busy_timeout=30000; PRAGMA journal_mode=WAL;";
        command.ExecuteNonQuery();
    }

    private static void RegisterFunctions(DbConnection connection)
    {
        if (connection is SqliteConnection sqliteConnection)
            sqliteConnection.CreateFunction("gen_random_uuid", () => Guid.NewGuid().ToString());
    }
}

public sealed class GuidKeyInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        AssignGuidKeys(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        AssignGuidKeys(eventData.Context);
        return ValueTask.FromResult(result);
    }

    private static void AssignGuidKeys(DbContext? context)
    {
        if (context is null) return;

        foreach (var entry in context.ChangeTracker.Entries().Where(entry => entry.State == EntityState.Added))
        {
            var primaryKey = entry.Metadata.FindPrimaryKey();
            if (primaryKey is null) continue;

            foreach (var property in primaryKey.Properties.Where(property => property.ClrType == typeof(Guid)))
            {
                var propertyEntry = entry.Property(property.Name);
                if (propertyEntry.CurrentValue is Guid value && value == Guid.Empty)
                    propertyEntry.CurrentValue = Guid.NewGuid();
            }
        }
    }
}
