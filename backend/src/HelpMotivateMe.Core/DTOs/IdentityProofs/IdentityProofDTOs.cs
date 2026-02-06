using HelpMotivateMe.Core.Enums;

namespace HelpMotivateMe.Core.DTOs.IdentityProofs;

// Request DTOs
public record CreateIdentityProofRequest(
    Guid IdentityId,
    string? Description,
    ProofIntensity Intensity = ProofIntensity.Easy
);

// Response DTOs
public record IdentityProofResponse(
    Guid Id,
    Guid IdentityId,
    string IdentityName,
    string? IdentityColor,
    string? IdentityIcon,
    DateOnly ProofDate,
    string? Description,
    ProofIntensity Intensity,
    int VoteValue,
    DateTime CreatedAt
);
