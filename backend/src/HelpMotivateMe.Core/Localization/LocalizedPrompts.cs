using HelpMotivateMe.Core.Enums;

namespace HelpMotivateMe.Core.Localization;

public static class LocalizedPrompts
{
    private static readonly Dictionary<Language, IPromptProvider> Providers = new()
    {
        { Language.English, new EnglishPromptProvider() },
        { Language.Danish, new DanishPromptProvider() }
    };

    public static string GetIdentityPrompt(Language language) =>
        GetProvider(language).IdentitySystemPrompt;

    public static string GetHabitStackPrompt(Language language) =>
        GetProvider(language).HabitStackSystemPrompt;

    public static string GetGoalsPrompt(Language language) =>
        GetProvider(language).GoalsSystemPrompt;

    public static string GetGeneralTaskCreationPrompt(Language language) =>
        GetProvider(language).GeneralTaskCreationPrompt;

    public static string GetPromptForStep(string step, Language language) =>
        step.ToLowerInvariant() switch
        {
            "identity" => GetIdentityPrompt(language),
            "habitstack" => GetHabitStackPrompt(language),
            "goal" => GetGoalsPrompt(language),
            _ => throw new ArgumentException($"Unknown step: {step}")
        };

    /// <summary>
    /// Builds the full system prompt with contextual information like current date.
    /// This helps the AI understand temporal references like "next week", "tomorrow", etc.
    /// </summary>
    public static string BuildSystemPrompt(string step, Language language, Dictionary<string, object>? context)
    {
        var basePrompt = GetPromptForStep(step, language);

        if (context == null || context.Count == 0)
        {
            return basePrompt;
        }

        // Build context section - use language-appropriate labels
        var contextLines = new List<string>
        {
            "",
            language == Language.Danish
                ? "NUVÆRENDE KONTEKST (brug dette til dato/tids referencer):"
                : "CURRENT CONTEXT (use this for date/time references):"
        };

        // Extract and format key context values
        if (context.TryGetValue("currentDateFormatted", out var dateFormatted))
        {
            var label = language == Language.Danish ? "Dagens dato" : "Today's date";
            contextLines.Add($"- {label}: {dateFormatted}");
        }
        else if (context.TryGetValue("currentDate", out var currentDate))
        {
            var label = language == Language.Danish ? "Dagens dato" : "Today's date";
            contextLines.Add($"- {label}: {currentDate}");
        }

        if (context.TryGetValue("currentYear", out var year))
        {
            var label = language == Language.Danish ? "Nuværende år" : "Current year";
            contextLines.Add($"- {label}: {year}");
        }

        if (context.TryGetValue("dayOfWeek", out var dayOfWeek))
        {
            var label = language == Language.Danish ? "Ugedag" : "Day of week";
            contextLines.Add($"- {label}: {dayOfWeek}");
        }

        if (context.TryGetValue("nextWeekDate", out var nextWeek))
        {
            var label = language == Language.Danish ? "'Næste uge' starter" : "'Next week' starts";
            contextLines.Add($"- {label}: {nextWeek}");
        }

        if (context.TryGetValue("nextMonthDate", out var nextMonth))
        {
            var label = language == Language.Danish ? "'Næste måned' starter" : "'Next month' starts";
            contextLines.Add($"- {label}: {nextMonth}");
        }

        if (context.TryGetValue("endOfYearDate", out var endOfYear))
        {
            var label = language == Language.Danish ? "Årets slutning" : "End of year";
            contextLines.Add($"- {label}: {endOfYear}");
        }

        if (context.TryGetValue("timeZone", out var timeZone))
        {
            var label = language == Language.Danish ? "Brugerens tidszone" : "User's timezone";
            contextLines.Add($"- {label}: {timeZone}");
        }

        contextLines.Add("");
        contextLines.Add(language == Language.Danish
            ? "Når brugeren nævner relative datoer som 'næste uge', 'næste måned', 'inden årets udgang', osv., brug ovenstående kontekst til at beregne den korrekte dato i ÅÅÅÅ-MM-DD format."
            : "When the user mentions relative dates like 'next week', 'next month', 'by end of year', etc., use the above context to calculate the correct date in YYYY-MM-DD format.");

        // Add user identities context for habit stack and goal steps
        if (context.TryGetValue("userIdentities", out var userIdentities))
        {
            contextLines.Add("");
            contextLines.Add(language == Language.Danish
                ? "BRUGERENS IDENTITETER (link vaner og mål til disse når relevant):"
                : "USER'S IDENTITIES (link habits and goals to these when relevant):");
            
            var identitiesJson = System.Text.Json.JsonSerializer.Serialize(userIdentities);
            contextLines.Add(identitiesJson);
            
            contextLines.Add("");
            contextLines.Add(language == Language.Danish
                ? "VIGTIGT: Når du opretter vane-stakke eller mål, prøv at matche dem med en af brugerens identiteter baseret på indholdet. Inkluder 'identityName' i JSON når der er et klart match. For eksempel: fitness vaner -> link til fitness/atlet identitet, læse vaner -> link til læser identitet."
                : "IMPORTANT: When creating habit stacks or goals, try to match them with one of the user's identities based on the content. Include 'identityName' in the JSON when there's a clear match. For example: fitness habits -> link to fitness/athlete identity, reading habits -> link to reader identity.");
        }

        return basePrompt + string.Join("\n", contextLines);
    }

    /// <summary>
    /// Builds the general task creation prompt with user identities and date context.
    /// </summary>
    public static string BuildGeneralCreationPrompt(Language language, Dictionary<string, object>? context)
    {
        var basePrompt = GetGeneralTaskCreationPrompt(language);

        // Replace {identities} placeholder with actual user identities
        if (context?.TryGetValue("identities", out var identitiesObj) == true)
        {
            var identitiesJson = System.Text.Json.JsonSerializer.Serialize(identitiesObj);
            basePrompt = basePrompt.Replace("{identities}", identitiesJson);
        }
        else
        {
            basePrompt = basePrompt.Replace("{identities}", "[]");
        }

        // Build context section for date/time references
        if (context == null || context.Count == 0)
        {
            return basePrompt;
        }

        var contextLines = new List<string>
        {
            "",
            language == Language.Danish
                ? "NUVÆRENDE KONTEKST (brug dette til dato/tids referencer):"
                : "CURRENT CONTEXT (use this for date/time references):"
        };

        // Extract and format key context values
        if (context.TryGetValue("currentDateFormatted", out var dateFormatted))
        {
            var label = language == Language.Danish ? "Dagens dato" : "Today's date";
            contextLines.Add($"- {label}: {dateFormatted}");
        }
        else if (context.TryGetValue("currentDate", out var currentDate))
        {
            var label = language == Language.Danish ? "Dagens dato" : "Today's date";
            contextLines.Add($"- {label}: {currentDate}");
        }

        if (context.TryGetValue("currentYear", out var year))
        {
            var label = language == Language.Danish ? "Nuværende år" : "Current year";
            contextLines.Add($"- {label}: {year}");
        }

        if (context.TryGetValue("dayOfWeek", out var dayOfWeek))
        {
            var label = language == Language.Danish ? "Ugedag" : "Day of week";
            contextLines.Add($"- {label}: {dayOfWeek}");
        }

        if (context.TryGetValue("nextWeekDate", out var nextWeek))
        {
            var label = language == Language.Danish ? "'Næste uge' starter" : "'Next week' starts";
            contextLines.Add($"- {label}: {nextWeek}");
        }

        if (context.TryGetValue("nextMonthDate", out var nextMonth))
        {
            var label = language == Language.Danish ? "'Næste måned' starter" : "'Next month' starts";
            contextLines.Add($"- {label}: {nextMonth}");
        }

        if (context.TryGetValue("endOfYearDate", out var endOfYear))
        {
            var label = language == Language.Danish ? "Årets slutning" : "End of year";
            contextLines.Add($"- {label}: {endOfYear}");
        }

        if (context.TryGetValue("timeZone", out var timeZone))
        {
            var label = language == Language.Danish ? "Brugerens tidszone" : "User's timezone";
            contextLines.Add($"- {label}: {timeZone}");
        }

        contextLines.Add("");
        contextLines.Add(language == Language.Danish
            ? "Når brugeren nævner relative datoer som 'i morgen', 'næste uge', 'næste måned', osv., brug ovenstående kontekst til at beregne den korrekte dato i ÅÅÅÅ-MM-DD format."
            : "When the user mentions relative dates like 'tomorrow', 'next week', 'next month', etc., use the above context to calculate the correct date in YYYY-MM-DD format.");

        return basePrompt + string.Join("\n", contextLines);
    }

    private static IPromptProvider GetProvider(Language language) =>
        Providers.GetValueOrDefault(language, Providers[Language.English]);
}
