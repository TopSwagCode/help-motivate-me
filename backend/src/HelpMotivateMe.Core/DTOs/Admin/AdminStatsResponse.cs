namespace HelpMotivateMe.Core.DTOs.Admin;

public record AdminStatsResponse(
    int TotalUsers,
    int ActiveUsers,
    int UsersLoggedInToday,
    MembershipStats MembershipStats,
    TaskTotals TaskTotals
);

public record MembershipStats(
    int FreeUsers,
    int PlusUsers,
    int ProUsers
);

public record TaskTotals(
    int TotalTasksCreated,
    int TotalTasksCompleted
);

public record DailyStatsResponse(
    string Date,
    int TasksCreated,
    int TasksCompleted,
    int TasksDue
);
