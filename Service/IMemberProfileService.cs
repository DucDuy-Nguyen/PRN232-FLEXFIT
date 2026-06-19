using Flexfit.DTOs.MemberProfile;
using System;
using System.Threading.Tasks;

namespace Flexfit.Service
{
    public interface IMemberProfileService
    {
        Task<MemberProfileResponse> GetProfileByUserIdAsync(Guid userId);
        Task<MemberProfileResponse> UpsertProfileAsync(Guid userId, UpdateMemberProfileRequest request);
    }
}