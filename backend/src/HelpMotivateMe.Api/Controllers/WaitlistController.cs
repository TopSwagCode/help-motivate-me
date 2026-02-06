using System.ComponentModel.DataAnnotations;
using HelpMotivateMe.Core.DTOs.Waitlist;
using HelpMotivateMe.Core.Entities;
using HelpMotivateMe.Core.Enums;
using HelpMotivateMe.Core.Interfaces;
using HelpMotivateMe.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HelpMotivateMe.Api.Controllers;

[ApiController]
[Route("api/waitlist")]
public class WaitlistController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IEmailService _emailService;

    public WaitlistController(AppDbContext db, IEmailService emailService)
    {
        _db = db;
        _emailService = emailService;
    }

    [HttpPost]
    public async Task<IActionResult> SignupForWaitlist([FromBody] WaitlistSignupRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || !new EmailAddressAttribute().IsValid(request.Email))
        {
            return BadRequest(new { message = "Please provide a valid email address" });
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { message = "Please provide your name" });
        }

        var email = request.Email.ToLowerInvariant().Trim();
        var name = request.Name.Trim();

        // Check if already on whitelist
        var isWhitelisted = await _db.WhitelistEntries.AnyAsync(w => w.Email.ToLower() == email);
        if (isWhitelisted)
        {
            return Ok(new { message = "Great news! You already have access. You can sign up or log in now.", canSignup = true });
        }

        // Check if already on waitlist
        var existingEntry = await _db.WaitlistEntries.FirstOrDefaultAsync(w => w.Email.ToLower() == email);
        if (existingEntry != null)
        {
            // Don't leak that they're already on the list - just return success
            return Ok(new { message = "Thank you for your interest! We'll notify you when a spot opens up." });
        }

        // Add to waitlist
        var entry = new WaitlistEntry
        {
            Email = email,
            Name = name
        };

        _db.WaitlistEntries.Add(entry);
        await _db.SaveChangesAsync();

        // Send confirmation email (default to English for non-registered users)
        await _emailService.SendWaitlistConfirmationAsync(email, name, Language.English);

        return Ok(new { message = "Thank you for your interest! We'll notify you when a spot opens up." });
    }

    [HttpGet("check")]
    public async Task<ActionResult<WhitelistCheckResponse>> CheckWhitelistStatus([FromQuery] string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return BadRequest(new { message = "Email is required" });
        }

        var normalizedEmail = email.ToLowerInvariant().Trim();
        var isWhitelisted = await _db.WhitelistEntries.AnyAsync(w => w.Email.ToLower() == normalizedEmail);

        return Ok(new WhitelistCheckResponse(isWhitelisted));
    }
}
