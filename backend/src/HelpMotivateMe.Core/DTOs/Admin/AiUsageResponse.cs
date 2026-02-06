namespace HelpMotivateMe.Core.DTOs.Admin;

public record AiUsageStatsResponse(
    decimal TotalEstimatedAllTime,
    decimal TotalActualAllTime,
    decimal TotalEstimatedLast30Days,
    decimal TotalActualLast30Days,
    decimal GlobalLimitLast30DaysUsd,
    decimal PerUserLimitLast30DaysUsd
);

public record AiUsageLogResponse(
    Guid Id,
    Guid UserId,
    string Email,
    string Model,
    int InputTokens,
    int OutputTokens,
    int? AudioDurationSeconds,
    decimal EstimatedCostUsd,
    decimal ActualCostUsd,
    string RequestType,
    bool Rejected,
    DateTime CreatedAt
);

public record PaginatedResponse<T>(
    IEnumerable<T> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
);