namespace HelpMotivateMe.Core.DTOs.Shared;

public record MessageResponse(string Message);

public record ErrorResponse(string Message, string? Code = null);

public record ErrorResponseWithEmail(string Message, string? Code = null, string? Email = null);
