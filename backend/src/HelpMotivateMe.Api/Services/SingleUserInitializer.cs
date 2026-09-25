using HelpMotivateMe.Core.Entities;
using HelpMotivateMe.Core.Options;
using HelpMotivateMe.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HelpMotivateMe.Api.Services;

public sealed class SingleUserInitializer : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IOptions<SingleUserOptions> _options;
    private readonly ILogger<SingleUserInitializer> _logger;

    public SingleUserInitializer(
        IServiceProvider serviceProvider,
        IOptions<SingleUserOptions> options,
        ILogger<SingleUserInitializer> logger)
    {
        _serviceProvider = serviceProvider;
        _options = options;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();

        var users = await db.Users.Take(2).ToListAsync(cancellationToken);
        if (users.Count > 1)
            throw new InvalidOperationException(
                "Single-user mode found more than one user. Restore a single-user backup or remove extra users before startup.");

        var configured = _options.Value;
        if (users.Count == 0)
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = configured.Username.Trim(),
                DisplayName = string.IsNullOrWhiteSpace(configured.DisplayName) ? null : configured.DisplayName.Trim(),
                IsActive = true,
            };
            user.PasswordHash = passwordHasher.HashPassword(user, configured.Password);
            db.Users.Add(user);
            await db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Provisioned the configured single user {Username}", user.Username);
            return;
        }

        var existing = users[0];
        var credentialsChanged = !string.Equals(existing.Username, configured.Username.Trim(), StringComparison.Ordinal) ||
                                 existing.PasswordHash is null ||
                                 passwordHasher.VerifyHashedPassword(existing, existing.PasswordHash, configured.Password) ==
                                 PasswordVerificationResult.Failed;

        existing.Username = configured.Username.Trim();
        existing.DisplayName = string.IsNullOrWhiteSpace(configured.DisplayName) ? null : configured.DisplayName.Trim();
        existing.IsActive = true;

        if (credentialsChanged)
        {
            existing.PasswordHash = passwordHasher.HashPassword(existing, configured.Password);
            existing.CredentialVersion++;
        }

        await db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Verified the configured single user {Username}", existing.Username);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
