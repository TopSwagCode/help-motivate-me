using System.Reflection;
using ImageMagick;

namespace HelpMotivateMe.Core.Localization.EmailTemplates;

/// <summary>
///     Provides email assets like the mailman mascot image as base64 encoded data URIs.
///     Images are compressed and cached on first access.
/// </summary>
public static class EmailAssets
{
    private static readonly Lazy<string> _mailmanBase64 = new(LoadAndCompressMailman);

    /// <summary>
    ///     Gets the mailman mascot image as a base64 data URI string.
    ///     The image is compressed to ~150px width and optimized for email.
    /// </summary>
    public static string MailmanDataUri => _mailmanBase64.Value;

    private static string LoadAndCompressMailman()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "HelpMotivateMe.Core.Assets.mailman.png";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
            // Return empty string if resource not found - emails will still work without image
            return string.Empty;

        using var image = new MagickImage(stream);

        // Resize to 150px width while maintaining aspect ratio
        image.Resize(150, 0);

        // Optimize for web/email
        image.Quality = 80;
        image.Strip(); // Remove metadata

        // Convert to PNG bytes
        var bytes = image.ToByteArray(MagickFormat.Png);
        var base64 = Convert.ToBase64String(bytes);

        return $"data:image/png;base64,{base64}";
    }
}