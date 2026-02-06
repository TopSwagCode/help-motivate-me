namespace HelpMotivateMe.Core.Options;

public class AiBudgetOptions
{
    public const string SectionName = "AiBudget";
    public decimal GlobalLimitLast30DaysUsd { get; set; } = 5.0m;
    public decimal PerUserLimitLast30DaysUsd { get; set; } = 0.25m;
}