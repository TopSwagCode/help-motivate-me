using HelpMotivateMe.Core.DTOs.IdentityProofs;
using HelpMotivateMe.Core.Entities;
using HelpMotivateMe.Core.Enums;
using HelpMotivateMe.Core.Interfaces;
using HelpMotivateMe.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HelpMotivateMe.Infrastructure.Services;

public class IdentityProofService : IIdentityProofService
{
    private readonly AppDbContext _context;
    private readonly IQueryInterface<Identity> _identities;
    private readonly IQueryInterface<IdentityProof> _proofs;
    private readonly IQueryInterface<NotificationPreferences> _notificationPreferences;

    public IdentityProofService(
        AppDbContext context,
        IQueryInterface<Identity> identities,
        IQueryInterface<IdentityProof> proofs,
        IQueryInterface<NotificationPreferences> notificationPreferences)
    {
        _context = context;
        _identities = identities;
        _proofs = proofs;
        _notificationPreferences = notificationPreferences;
    }

    /// <summary>
    /// Create a new identity proof.
    /// </summary>
    public async Task<IdentityProofResponse> CreateProofAsync(Guid userId, CreateIdentityProofRequest request)
    {
        // Verify identity belongs to user
        var identity = await _identities.FirstOrDefaultAsync(i => i.Id == request.IdentityId && i.UserId == userId);
        if (identity == null)
        {
            throw new InvalidOperationException("Invalid identity.");
        }

        var today = await GetUserLocalDateAsync(userId);

        var proof = new IdentityProof
        {
            UserId = userId,
            IdentityId = request.IdentityId,
            ProofDate = today,
            Description = request.Description?.Trim(),
            Intensity = request.Intensity
        };

        _context.IdentityProofs.Add(proof);
        await _context.SaveChangesAsync();

        // Reload with identity for response
        await _context.Entry(proof).Reference(p => p.Identity).LoadAsync();

        return MapToResponse(proof);
    }

    /// <summary>
    /// Get proofs for a date range.
    /// </summary>
    public async Task<IEnumerable<IdentityProofResponse>> GetProofsAsync(Guid userId, DateOnly? startDate, DateOnly? endDate)
    {
        var query = _proofs
            .Include(p => p.Identity)
            .Where(p => p.UserId == userId);

        if (startDate.HasValue)
        {
            query = query.Where(p => p.ProofDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(p => p.ProofDate <= endDate.Value);
        }

        var proofs = await query
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return proofs.Select(MapToResponse);
    }

    /// <summary>
    /// Delete a proof.
    /// </summary>
    public async Task<bool> DeleteProofAsync(Guid userId, Guid proofId)
    {
        var proof = await _context.IdentityProofs
            .FirstOrDefaultAsync(p => p.Id == proofId && p.UserId == userId);

        if (proof == null)
        {
            return false;
        }

        _context.IdentityProofs.Remove(proof);
        await _context.SaveChangesAsync();

        return true;
    }

    private static IdentityProofResponse MapToResponse(IdentityProof proof)
    {
        return new IdentityProofResponse(
            proof.Id,
            proof.IdentityId,
            proof.Identity.Name,
            proof.Identity.Color,
            proof.Identity.Icon,
            proof.ProofDate,
            proof.Description,
            proof.Intensity,
            (int)proof.Intensity,
            proof.CreatedAt
        );
    }

    /// <summary>
    /// Resolves the user's current local date based on their timezone settings.
    /// </summary>
    private async Task<DateOnly> GetUserLocalDateAsync(Guid userId)
    {
        var preferences = await _notificationPreferences
            .FirstOrDefaultAsync(np => np.UserId == userId);

        if (preferences == null)
        {
            // Fallback to UTC if no preferences set
            return DateOnly.FromDateTime(DateTime.UtcNow);
        }

        var localDateTime = ResolveLocalTime(DateTime.UtcNow, preferences.TimezoneId, preferences.UtcOffsetMinutes);
        return DateOnly.FromDateTime(localDateTime);
    }

    /// <summary>
    /// Resolves the user's local time from UTC using their timezone settings.
    /// </summary>
    private static DateTime ResolveLocalTime(DateTime utcNow, string timezoneId, int utcOffsetMinutes)
    {
        // Try to use TimezoneId first (authoritative)
        try
        {
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
            return TimeZoneInfo.ConvertTimeFromUtc(utcNow, timeZone);
        }
        catch (TimeZoneNotFoundException)
        {
            // Fall back to UTC offset
            return utcNow.AddMinutes(utcOffsetMinutes);
        }
        catch (InvalidTimeZoneException)
        {
            // Fall back to UTC offset
            return utcNow.AddMinutes(utcOffsetMinutes);
        }
    }
}
