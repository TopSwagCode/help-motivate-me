namespace HelpMotivateMe.Core.DTOs.Admin;

public record AdminStatsResponse(
    int TotalUsers,
    int ActiveUsers,
    MembershipStats MembershipStats,
    TodayStats TodayStats
);

public record MembershipStats(
    int FreeUsers,
    int PlusUsers,
    int ProUsers
);

public record TodayStats(
    int TasksCreated,
    int TasksUpdated,
    int TasksCompleted
);
