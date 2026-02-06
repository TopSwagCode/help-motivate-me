using HelpMotivateMe.Core.Entities;
using HelpMotivateMe.Core.Enums;
using HelpMotivateMe.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HelpMotivateMe.Api.Services;

public class AdminUserSeeder : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AdminUserSeeder> _logger;

    public AdminUserSeeder(IServiceProvider serviceProvider, ILogger<AdminUserSeeder> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        const string adminEmail = "joshua.ryder@outlook.com";

        var existingUser = await db.Users
            .FirstOrDefaultAsync(u => u.Email == adminEmail, cancellationToken);

        if (existingUser == null)
        {
            var adminUser = new User
            {
                Id = Guid.NewGuid(),
                Email = adminEmail,
                DisplayName = "Admin",
                IsActive = true,
                Role = UserRole.Admin,
                MembershipTier = MembershipTier.Pro,
                HasCompletedOnboarding = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            db.Users.Add(adminUser);
            await db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Created admin user with email {Email}", adminEmail);
        }
        else if (existingUser.Role != UserRole.Admin)
        {
            existingUser.Role = UserRole.Admin;
            await db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Updated existing user {Email} to Admin role", adminEmail);
        }
        else
        {
            _logger.LogInformation("Admin user {Email} already exists with Admin role", adminEmail);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
