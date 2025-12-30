namespace HelpMotivateMe.Core.Interfaces;

public interface IStorageService
{
    Task<string> UploadAsync(Stream stream, string key, string contentType, CancellationToken cancellationToken = default);
    Task DeleteAsync(string key, CancellationToken cancellationToken = default);
    Task DeleteManyAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default);
    string GetPresignedUrl(string key);
}
