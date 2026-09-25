using HelpMotivateMe.Core.Entities;
using HelpMotivateMe.Core.Interfaces;
using HelpMotivateMe.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HelpMotivateMe.Infrastructure.Services;

public sealed class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher<User> _passwordHasher;

    public AuthService(AppDbContext db, IPasswordHasher<User> passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    public Task<User?> GetUserByUsernameAsync(string username)
    {
        var normalized = username.Trim().ToUpperInvariant();
        return _db.Users.SingleOrDefaultAsync(user => user.Username.ToUpper() == normalized);
    }

    public Task<User?> GetUserByIdAsync(Guid userId) =>
        _db.Users.SingleOrDefaultAsync(user => user.Id == userId);

    public bool VerifyPassword(User user, string password) =>
        user.PasswordHash is not null &&
        _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password) != PasswordVerificationResult.Failed;

    public async Task UpdateUserAsync(User user)
    {
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }
}
