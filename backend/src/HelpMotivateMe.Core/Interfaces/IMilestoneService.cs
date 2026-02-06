using HelpMotivateMe.Core.DTOs.Milestones;

namespace HelpMotivateMe.Core.Interfaces;

public interface IMilestoneService
{
    /// <summary>
    ///     Record a domain event, update user stats, and evaluate milestones.
    ///     Returns any newly awarded milestones.
    /// </summary>
    Task<List<UserMilestoneResponse>> RecordEventAsync(Guid userId, string eventType, object? metadata = null);

    /// <summary>
    ///     Get all milestones awarded to a user.
    /// </summary>
    Task<List<UserMilestoneResponse>> GetUserMilestonesAsync(Guid userId);

    /// <summary>
    ///     Get milestones that haven't been seen yet.
    /// </summary>
    Task<List<UserMilestoneResponse>> GetUnseenMilestonesAsync(Guid userId);

    /// <summary>
    ///     Mark milestones as seen.
    /// </summary>
    Task MarkMilestonesSeenAsync(Guid userId, List<Guid> milestoneIds);

    /// <summary>
    ///     Get all milestone definitions (admin).
    /// </summary>
    Task<List<MilestoneDefinitionResponse>> GetAllDefinitionsAsync();

    /// <summary>
    ///     Get user stats.
    /// </summary>
    Task<UserStatsResponse> GetUserStatsAsync(Guid userId);

    /// <summary>
    ///     Create a new milestone definition (admin).
    /// </summary>
    Task<MilestoneDefinitionResponse> CreateDefinitionAsync(CreateMilestoneRequest request);

    /// <summary>
    ///     Update an existing milestone definition (admin).
    /// </summary>
    Task<MilestoneDefinitionResponse?> UpdateDefinitionAsync(Guid id, UpdateMilestoneRequest request);

    /// <summary>
    ///     Toggle milestone active status (admin).
    /// </summary>
    Task<bool> ToggleDefinitionAsync(Guid id, bool isActive);

    /// <summary>
    ///     Delete a milestone definition (admin).
    /// </summary>
    Task<bool> DeleteDefinitionAsync(Guid id);
}