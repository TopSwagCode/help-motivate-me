namespace HelpMotivateMe.Core.Exceptions;

public class AiBudgetExceededException : Exception
{
    public string Reason { get; }

    public AiBudgetExceededException(string reason)
        : base($"AI budget exceeded: {reason}")
    {
        Reason = reason;
    }
}
