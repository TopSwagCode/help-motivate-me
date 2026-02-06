namespace HelpMotivateMe.Core.Exceptions;

public class AiBudgetExceededException : Exception
{
    public AiBudgetExceededException(string reason)
        : base($"AI budget exceeded: {reason}")
    {
        Reason = reason;
    }

    public string Reason { get; }
}