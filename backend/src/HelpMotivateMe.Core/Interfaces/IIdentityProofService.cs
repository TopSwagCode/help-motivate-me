using HelpMotivateMe.Core.DTOs.IdentityProofs;

namespace HelpMotivateMe.Core.Interfaces;

public interface IIdentityProofService
{
    Task<IdentityProofResponse> CreateProofAsync(Guid userId, CreateIdentityProofRequest request);
    Task<IEnumerable<IdentityProofResponse>> GetProofsAsync(Guid userId, DateOnly? startDate, DateOnly? endDate);
    Task<bool> DeleteProofAsync(Guid userId, Guid proofId);
}
