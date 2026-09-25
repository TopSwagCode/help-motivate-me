using HelpMotivateMe.Core.Constants;
using HelpMotivateMe.Core.Enums;
using HelpMotivateMe.Core.Localization;

namespace HelpMotivateMe.IntegrationTests.Localization;

public class AiHabitStackPromptTests
{
    public static TheoryData<Language> SupportedLanguages => new()
    {
        Language.English,
        Language.Danish
    };

    [Theory]
    [MemberData(nameof(SupportedLanguages))]
    public void HabitStackOnboardingPrompt_DescribesCurrentScheduleContract(Language language)
    {
        var prompt = LocalizedPrompts.GetHabitStackPrompt(language);

        AssertScheduleContract(prompt);
        prompt.Should().Contain("\"oddWeekDays\":127");
        prompt.Should().Contain("\"evenWeekDays\":127");
    }

    [Theory]
    [MemberData(nameof(SupportedLanguages))]
    public void GeneralCreationPrompt_DescribesCurrentScheduleContract(Language language)
    {
        var prompt = LocalizedPrompts.GetGeneralTaskCreationPrompt(language);

        AssertScheduleContract(prompt);
        prompt.Should().Contain("\"oddWeekDays\":127");
        prompt.Should().Contain("\"evenWeekDays\":127");
    }

    [Fact]
    public void LegacyOnboardingPrompt_DescribesCurrentScheduleContract()
    {
        var prompt = OnboardingPrompts.HabitStackSystemPrompt;

        AssertScheduleContract(prompt);
        prompt.Should().Contain("\"oddWeekDays\":127");
        prompt.Should().Contain("\"evenWeekDays\":127");
    }

    private static void AssertScheduleContract(string prompt)
    {
        prompt.Should().Contain("oddWeekDays=3");
        prompt.Should().Contain("evenWeekDays=115");
        prompt.Should().Contain("ISO");
        (prompt.Contains("Monday=1") || prompt.Contains("mandag=1")).Should().BeTrue();
        (prompt.Contains("Sunday=64") || prompt.Contains("søndag=64")).Should().BeTrue();
    }
}