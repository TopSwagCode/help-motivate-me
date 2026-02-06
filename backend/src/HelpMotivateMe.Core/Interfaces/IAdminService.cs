using HelpMotivateMe.Core.DTOs.Admin;
using HelpMotivateMe.Core.DTOs.Waitlist;

namespace HelpMotivateMe.Core.Interfaces;

public interface IAdminService
{
    // Stats
    Task<AdminStatsResponse> GetStatsAsync();
    Task<DailyStatsResponse> GetDailyStatsAsync(DateOnly date);

    // Users
    Task<(List<AdminUserResponse> Users, int TotalCount)> GetUsersAsync(
        string? search, string? tier, bool? isActive, int page, int pageSize);
    Task<AdminUserResponse?> ToggleUserActiveAsync(Guid userId);
    Task<AdminUserResponse?> UpdateUserRoleAsync(Guid userId, string role);
    Task<UserActivityResponse?> GetUserActivityAsync(Guid userId);

    // Waitlist
    Task<(List<WaitlistEntryResponse> Entries, int TotalCount)> GetWaitlistAsync(
        string? search, int page, int pageSize);
    Task<bool> RemoveFromWaitlistAsync(Guid id);
    Task<WhitelistEntryResponse?> ApproveWaitlistEntryAsync(Guid id, Guid? currentUserId);

    // Whitelist
    Task<(List<WhitelistEntryResponse> Entries, int TotalCount)> GetWhitelistAsync(
        string? search, int page, int pageSize);
    Task<(WhitelistEntryResponse? Entry, string? Error)> AddToWhitelistAsync(
        string email, Guid? currentUserId);
    Task<bool> RemoveFromWhitelistAsync(Guid id);

    // Analytics
    Task<AnalyticsOverviewResponse> GetAnalyticsOverviewAsync(int days);

    // AI Usage
    Task<AiUsageStatsResponse> GetAiUsageStatsAsync();
    Task<PaginatedResponse<AiUsageLogResponse>> GetAiUsageLogsAsync(int page, int pageSize);

    // Settings
    bool GetAllowSignups();
}
