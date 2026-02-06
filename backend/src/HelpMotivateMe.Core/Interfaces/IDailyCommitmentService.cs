using HelpMotivateMe.Core.DTOs.DailyCommitment;

namespace HelpMotivateMe.Core.Interfaces;

public interface IDailyCommitmentService
{
    Task<DailyCommitmentResponse?> GetCommitmentAsync(Guid userId, DateOnly date);
    Task<CommitmentOptionsResponse> GetIdentityOptionsAsync(Guid userId, DateOnly date);
    Task<ActionSuggestionsResponse> GetActionSuggestionsAsync(Guid userId, Guid identityId);
    Task<DailyCommitmentResponse> CreateCommitmentAsync(Guid userId, CreateDailyCommitmentRequest request);
    Task<DailyCommitmentResponse> CompleteCommitmentAsync(Guid userId, Guid commitmentId);
    Task<DailyCommitmentResponse> DismissCommitmentAsync(Guid userId, Guid commitmentId);
    Task<YesterdayCommitmentResponse> GetYesterdayCommitmentAsync(Guid userId);
    Task<bool> CheckAndAutoCompleteForHabitAsync(Guid userId, Guid habitStackItemId);
    Task<bool> CheckAndAutoCompleteForTaskAsync(Guid userId, Guid taskId);
}
