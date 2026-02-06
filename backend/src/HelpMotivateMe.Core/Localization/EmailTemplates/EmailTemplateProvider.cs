using HelpMotivateMe.Core.Enums;

namespace HelpMotivateMe.Core.Localization.EmailTemplates;

public static class EmailTemplateProvider
{
    private static readonly Dictionary<Language, IEmailTemplates> Templates = new()
    {
        { Language.English, new EnglishEmailTemplates() },
        { Language.Danish, new DanishEmailTemplates() }
    };

    public static IEmailTemplates GetTemplates(Language language) =>
        Templates.GetValueOrDefault(language, Templates[Language.English]);
}
