namespace HelpMotivateMe.Core.Localization.EmailTemplates;

/// <summary>
/// Provides the shared base template for all emails with consistent branding.
/// Colors are derived from the frontend's Tailwind design system.
/// </summary>
public static class EmailTemplateBase
{
    // Brand colors from frontend tailwind.config.js
    public const string PrimaryColor = "#d4944c";      // primary-500 (caramel)
    public const string PrimaryHover = "#b87a3a";      // primary-600
    public const string BackgroundColor = "#faf7f2";   // warm-cream
    public const string CardBackground = "#fdfcfa";    // warm-paper
    public const string TextPrimary = "#5f483d";       // cocoa-800
    public const string TextSecondary = "#735748";     // cocoa-700
    public const string TextMuted = "#8a6a54";         // cocoa-600
    public const string AccentGreen = "#768862";       // sage-500
    public const string BorderColor = "#f5f0e8";       // warm-beige
    public const string HighlightBg = "#fdf6ed";       // primary-100

    // Font stack matching frontend
    public const string FontFamily = "'Nunito', 'Inter', system-ui, -apple-system, 'Segoe UI', Roboto, sans-serif";

    /// <summary>
    /// Frontend URL for hosted assets. Set this at application startup.
    /// When set, emails will use hosted images instead of base64 (better email client compatibility).
    /// </summary>
    public static string? FrontendUrl { get; set; }

    /// <summary>
    /// Wraps email content in a branded HTML template with header (mailman mascot) and footer.
    /// </summary>
    /// <param name="title">The main heading for the email</param>
    /// <param name="bodyContent">The inner HTML content of the email body</param>
    /// <returns>Complete HTML email document</returns>
    public static string WrapContent(string title, string bodyContent)
    {
        // Use hosted image URL if FrontendUrl is configured (better email client compatibility)
        // Fall back to base64 encoded image if not configured
        var mailmanSrc = !string.IsNullOrEmpty(FrontendUrl)
            ? $"{FrontendUrl.TrimEnd('/')}/mailman.png"
            : EmailAssets.MailmanDataUri;
        var mailmanHtml = !string.IsNullOrEmpty(mailmanSrc)
            ? $@"<img src='{mailmanSrc}' alt='Help Motivate Me Mascot' style='width: 100px; height: auto; margin-bottom: 16px;' />"
            : string.Empty;

        return $@"<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>{title}</title>
</head>
<body style='margin: 0; padding: 0; background-color: {BackgroundColor}; font-family: {FontFamily};'>
    <table role='presentation' cellpadding='0' cellspacing='0' width='100%' style='background-color: {BackgroundColor};'>
        <tr>
            <td align='center' style='padding: 40px 20px;'>
                <table role='presentation' cellpadding='0' cellspacing='0' width='600' style='max-width: 600px; background-color: {CardBackground}; border-radius: 16px; box-shadow: 0 4px 6px rgba(0, 0, 0, 0.05);'>
                    <!-- Header with mascot -->
                    <tr>
                        <td align='center' style='padding: 32px 40px 16px 40px;'>
                            {mailmanHtml}
                        </td>
                    </tr>
                    <!-- Title -->
                    <tr>
                        <td align='center' style='padding: 0 40px 24px 40px;'>
                            <h1 style='margin: 0; font-size: 28px; font-weight: 700; color: {TextPrimary}; line-height: 1.3;'>{title}</h1>
                        </td>
                    </tr>
                    <!-- Body content -->
                    <tr>
                        <td style='padding: 0 40px 32px 40px; color: {TextSecondary}; font-size: 16px; line-height: 1.6;'>
                            {bodyContent}
                        </td>
                    </tr>
                    <!-- Footer -->
                    <tr>
                        <td style='padding: 24px 40px; border-top: 1px solid {BorderColor}; text-align: center;'>
                            <p style='margin: 0; font-size: 14px; color: {TextMuted};'>
                                Help Motivate Me — Your personal growth companion
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
    }

    /// <summary>
    /// Creates a styled primary button HTML.
    /// </summary>
    public static string CreateButton(string text, string url) => $@"
        <table role='presentation' cellpadding='0' cellspacing='0' style='margin: 24px 0;'>
            <tr>
                <td align='center' style='background-color: {PrimaryColor}; border-radius: 8px;'>
                    <a href='{url}' target='_blank' style='display: inline-block; padding: 14px 32px; font-size: 16px; font-weight: 600; color: #ffffff; text-decoration: none; font-family: {FontFamily};'>
                        {text}
                    </a>
                </td>
            </tr>
        </table>";

    /// <summary>
    /// Creates a styled info box with highlighted background.
    /// </summary>
    public static string CreateInfoBox(string content) => $@"
        <div style='background-color: {HighlightBg}; padding: 20px; border-radius: 12px; margin: 20px 0; border-left: 4px solid {PrimaryColor};'>
            {content}
        </div>";

    /// <summary>
    /// Creates a styled quote/message box.
    /// </summary>
    public static string CreateQuoteBox(string quote) => $@"
        <div style='background-color: {HighlightBg}; padding: 20px; border-radius: 12px; margin: 20px 0;'>
            <p style='font-style: italic; margin: 0; color: {TextPrimary}; font-size: 16px;'>""{quote}""</p>
        </div>";

    /// <summary>
    /// Creates the fallback link text for when buttons don't work.
    /// </summary>
    public static string CreateFallbackLink(string url) => $@"
        <p style='font-size: 14px; color: {TextMuted}; margin-top: 24px;'>
            If the button doesn't work, copy and paste this link into your browser:<br/>
            <a href='{url}' style='color: {PrimaryColor}; word-break: break-all;'>{url}</a>
        </p>";

    /// <summary>
    /// Creates the fallback link text in Danish.
    /// </summary>
    public static string CreateFallbackLinkDanish(string url) => $@"
        <p style='font-size: 14px; color: {TextMuted}; margin-top: 24px;'>
            Hvis knappen ikke virker, kopier og indsæt dette link i din browser:<br/>
            <a href='{url}' style='color: {PrimaryColor}; word-break: break-all;'>{url}</a>
        </p>";
}
