using System.Text.Json;
using HelpMotivateMe.Core.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.Sqlite;
using WebPush;

namespace HelpMotivateMe.Infrastructure.Services;

public static class VapidKeyStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public static VapidKeyMaterial LoadOrCreate(IConfiguration configuration)
    {
        var options = configuration.GetSection(VapidOptions.SectionName).Get<VapidOptions>() ?? new VapidOptions();
        var hasPublicKey = !string.IsNullOrWhiteSpace(options.PublicKey);
        var hasPrivateKey = !string.IsNullOrWhiteSpace(options.PrivateKey);

        if (hasPublicKey != hasPrivateKey)
            throw new InvalidOperationException("Vapid:PublicKey and Vapid:PrivateKey must either both be set or both be omitted.");

        if (hasPublicKey)
            return new VapidKeyMaterial(options.PublicKey!.Trim(), options.PrivateKey!.Trim(), options.Subject);

        var keyFilePath = ResolveKeyFilePath(options, configuration);
        if (File.Exists(keyFilePath)) return Read(keyFilePath, options.Subject);

        var directory = Path.GetDirectoryName(keyFilePath)
                        ?? throw new InvalidOperationException("Vapid:KeyFilePath must include a directory.");
        Directory.CreateDirectory(directory);

        var generatedKeys = VapidHelper.GenerateVapidKeys();
        var stored = new StoredVapidKeys(generatedKeys.PublicKey, generatedKeys.PrivateKey);
        var temporaryPath = $"{keyFilePath}.{Guid.NewGuid():N}.tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(stored, JsonOptions));
        try
        {
            File.Move(temporaryPath, keyFilePath, false);
        }
        catch (IOException) when (File.Exists(keyFilePath))
        {
            File.Delete(temporaryPath);
            return Read(keyFilePath, options.Subject);
        }

        if (!OperatingSystem.IsWindows())
            File.SetUnixFileMode(keyFilePath, UnixFileMode.UserRead | UnixFileMode.UserWrite);

        return new VapidKeyMaterial(stored.PublicKey, stored.PrivateKey, options.Subject);
    }

    private static VapidKeyMaterial Read(string keyFilePath, string subject)
    {
        var storedKeys = JsonSerializer.Deserialize<StoredVapidKeys>(File.ReadAllText(keyFilePath), JsonOptions)
                         ?? throw new InvalidOperationException($"VAPID key file '{keyFilePath}' is empty or invalid.");
        if (string.IsNullOrWhiteSpace(storedKeys.PublicKey) || string.IsNullOrWhiteSpace(storedKeys.PrivateKey))
            throw new InvalidOperationException($"VAPID key file '{keyFilePath}' does not contain a complete key pair.");

        return new VapidKeyMaterial(storedKeys.PublicKey, storedKeys.PrivateKey, subject);
    }

    private static string ResolveKeyFilePath(VapidOptions options, IConfiguration configuration)
    {
        if (!string.IsNullOrWhiteSpace(options.KeyFilePath)) return Path.GetFullPath(options.KeyFilePath);

        var connectionString = configuration.GetConnectionString("DefaultConnection")
                               ?? throw new InvalidOperationException("Vapid:KeyFilePath or ConnectionStrings:DefaultConnection is required.");
        var dataSource = new SqliteConnectionStringBuilder(connectionString).DataSource;
        if (string.IsNullOrWhiteSpace(dataSource) || dataSource == ":memory:")
            throw new InvalidOperationException("Vapid:KeyFilePath is required when using an in-memory database.");

        var databasePath = Path.GetFullPath(dataSource);
        return Path.Combine(Path.GetDirectoryName(databasePath)!, "vapid-keys.json");
    }

    private sealed record StoredVapidKeys(string PublicKey, string PrivateKey);
}