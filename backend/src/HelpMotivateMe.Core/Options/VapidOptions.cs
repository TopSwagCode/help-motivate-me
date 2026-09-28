namespace HelpMotivateMe.Core.Options;

public sealed class VapidOptions
{
    public const string SectionName = "Vapid";

    public string? PublicKey { get; init; }
    public string? PrivateKey { get; init; }
    public string Subject { get; init; } = "mailto:admin@helpmotivateme.app";
    public string? KeyFilePath { get; init; }
}

public sealed record VapidKeyMaterial(string PublicKey, string PrivateKey, string Subject);