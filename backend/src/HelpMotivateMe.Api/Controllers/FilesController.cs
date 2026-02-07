using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;

namespace HelpMotivateMe.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/files")]
public class FilesController : ControllerBase
{
    private readonly string _basePath;
    private readonly FileExtensionContentTypeProvider _contentTypeProvider;
    private readonly ILogger<FilesController> _logger;

    public FilesController(IConfiguration configuration, ILogger<FilesController> logger)
    {
        _basePath = configuration["LocalStorage:BasePath"]
                    ?? throw new InvalidOperationException("LocalStorage:BasePath not configured");
        _logger = logger;
        _contentTypeProvider = new FileExtensionContentTypeProvider();
    }

    [HttpGet("{*filepath}")]
    public IActionResult GetFile(string filepath)
    {
        if (string.IsNullOrWhiteSpace(filepath)) return BadRequest("File path is required");

        // Strip any existing api/files/ prefix to handle legacy data with bad keys
        var cleanPath = filepath.StartsWith("api/files/") ? filepath.Substring("api/files/".Length) : filepath;
        var fullPath = Path.Combine(_basePath, cleanPath);

        // Ensure the resolved path is still within the base path (security check)
        var resolvedPath = Path.GetFullPath(fullPath);
        var resolvedBasePath = Path.GetFullPath(_basePath + Path.DirectorySeparatorChar);

        if (!resolvedPath.StartsWith(resolvedBasePath))
        {
            _logger.LogWarning("Attempted path traversal attack: {FilePath}", filepath);
            return BadRequest("Invalid file path");
        }

        if (!System.IO.File.Exists(fullPath))
        {
            _logger.LogWarning("File not found: {FilePath}", filepath);
            return NotFound();
        }

        try
        {
            // Determine content type
            if (!_contentTypeProvider.TryGetContentType(fullPath, out var contentType))
                contentType = "application/octet-stream";

            return PhysicalFile(fullPath, contentType, true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error serving file: {FilePath}", filepath);
            return StatusCode(500, "Error serving file");
        }
    }
}