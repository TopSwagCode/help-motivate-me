using System.ComponentModel.DataAnnotations;

namespace HelpMotivateMe.Core.DTOs.Journal;

public record UpdateJournalEntryRequest(
    [Required] [StringLength(255)] string Title,
    string? Description,
    DateOnly EntryDate,
    Guid? HabitStackId,
    Guid? TaskItemId
);