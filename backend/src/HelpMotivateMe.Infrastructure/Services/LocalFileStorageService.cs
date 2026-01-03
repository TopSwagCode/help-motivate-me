using HelpMotivateMe.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HelpMotivateMe.Infrastructure.Services;

public class LocalFileStorageService : IStorageService
{
    private readonly string _basePath;
    private readonly string _baseUrl;
    private readonly ILogger<LocalFileStorageService> _logger;

    public LocalFileStorageService(IConfiguration configuration, ILogger<LocalFileStorageService> logger)
    {
        _basePath = configuration["LocalStorage:BasePath"]
            ?? throw new InvalidOperationException("LocalStorage:BasePath not configured");
        
        _baseUrl = configuration["LocalStorage:BaseUrl"]
            ?? throw new InvalidOperationException("LocalStorage:BaseUrl not configured");
        
        _logger = logger;

        // Ensure the base directory exists
        if (!Directory.Exists(_basePath))
        {
            Directory.CreateDirectory(_basePath);
            _logger.LogInformation("Created storage directory: {BasePath}", _basePath);
        }
    }

    public async Task<string> UploadAsync(Stream stream, string key, string contentType, CancellationToken cancellationToken = default)
    {
        var filePath = GetFullPath(key);
        var directory = Path.GetDirectoryName(filePath);

        if (directory != null && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        try
        {
            await using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true);
            await stream.CopyToAsync(fileStream, cancellationToken);
            
            _logger.LogInformation("Uploaded file: {Key} to {FilePath}", key, filePath);
            return key;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file: {Key}", key);
            throw;
        }
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        var filePath = GetFullPath(key);

        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.LogInformation("Deleted file: {Key}", key);
            }
            else
            {
                _logger.LogWarning("Attempted to delete non-existent file: {Key}", key);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file: {Key}", key);
            throw;
        }

        return Task.CompletedTask;
    }

    public Task DeleteManyAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default)
    {
        var keyList = keys.ToList();
        if (keyList.Count == 0) return Task.CompletedTask;

        var errors = new List<Exception>();

        foreach (var key in keyList)
        {
            try
            {
                var filePath = GetFullPath(key);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete file: {Key}", key);
                errors.Add(ex);
            }
        }

        if (errors.Count > 0)
        {
            throw new AggregateException("Failed to delete one or more files", errors);
        }

        _logger.LogInformation("Deleted {Count} files", keyList.Count);
        return Task.CompletedTask;
    }

    public string GetPresignedUrl(string key)
    {
        // For local storage, return a direct URL to the file serving endpoint
        // The URL format will be: {baseUrl}/api/files/{key}
        // Strip any existing api/files/ prefix to handle legacy data
        var cleanKey = key.StartsWith("api/files/") ? key.Substring("api/files/".Length) : key;
        return $"{_baseUrl.TrimEnd('/')}/api/files/{cleanKey}";
    }

    private string GetFullPath(string key)
    {
        // Strip any existing api/files/ prefix to handle legacy data
        var cleanKey = key.StartsWith("api/files/") ? key.Substring("api/files/".Length) : key;
        // Normalize the key to prevent path traversal attacks
        var normalizedKey = cleanKey.Replace("..", "").Replace("\\", "/");
        return Path.Combine(_basePath, normalizedKey);
    }
}
