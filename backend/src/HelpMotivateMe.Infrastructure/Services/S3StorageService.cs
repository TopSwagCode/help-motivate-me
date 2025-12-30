using Amazon.S3;
using Amazon.S3.Model;
using HelpMotivateMe.Core.Interfaces;
using Microsoft.Extensions.Configuration;

namespace HelpMotivateMe.Infrastructure.Services;

public class S3StorageService : IStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;
    private readonly int _presignedUrlExpiryMinutes;
    private readonly bool _useLocalStack;

    public S3StorageService(IAmazonS3 s3Client, IConfiguration configuration)
    {
        _s3Client = s3Client;
        _bucketName = configuration["S3:BucketName"]
            ?? throw new InvalidOperationException("S3:BucketName not configured");
        _presignedUrlExpiryMinutes = int.TryParse(configuration["S3:PresignedUrlExpiryMinutes"], out var expiry) ? expiry : 60;
        _useLocalStack = bool.TryParse(configuration["S3:UseLocalStack"], out var useLocalStack) && useLocalStack;
    }

    public async Task<string> UploadAsync(Stream stream, string key, string contentType, CancellationToken cancellationToken = default)
    {
        var request = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = key,
            InputStream = stream,
            ContentType = contentType
        };

        await _s3Client.PutObjectAsync(request, cancellationToken);
        return key;
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        await _s3Client.DeleteObjectAsync(_bucketName, key, cancellationToken);
    }

    public async Task DeleteManyAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default)
    {
        var keyList = keys.ToList();
        if (keyList.Count == 0) return;

        var request = new DeleteObjectsRequest
        {
            BucketName = _bucketName,
            Objects = keyList.Select(k => new KeyVersion { Key = k }).ToList()
        };

        await _s3Client.DeleteObjectsAsync(request, cancellationToken);
    }

    public string GetPresignedUrl(string key)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucketName,
            Key = key,
            Expires = DateTime.UtcNow.AddMinutes(_presignedUrlExpiryMinutes),
            Verb = HttpVerb.GET,
            Protocol = _useLocalStack ? Protocol.HTTP : Protocol.HTTPS
        };

        return _s3Client.GetPreSignedURL(request);
    }
}
