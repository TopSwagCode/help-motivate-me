using HelpMotivateMe.Infrastructure.Data;
using HelpMotivateMe.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HelpMotivateMe.IntegrationTests.Infrastructure;

public class SqliteSchemaUpdaterTests
{
    [Fact]
    public async Task UpgradeAsync_AddsHabitStackScheduleColumnsWithDailyDefaults()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        await using (var createCommand = connection.CreateCommand())
        {
            createCommand.CommandText = "CREATE TABLE habit_stacks (Id TEXT NOT NULL PRIMARY KEY);";
            await createCommand.ExecuteNonQueryAsync();
        }

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var db = new AppDbContext(options);

        await SqliteSchemaUpdater.UpgradeAsync(db);

        var columns = new Dictionary<string, string>();
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA table_info('habit_stacks');";
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync()) columns[reader.GetString(1)] = reader.GetValue(4).ToString()!;

        columns.Should().Contain("OddWeekDays", "127");
        columns.Should().Contain("EvenWeekDays", "127");
    }
}
