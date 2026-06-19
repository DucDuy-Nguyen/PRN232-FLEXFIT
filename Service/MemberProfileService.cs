using Flexfit.DTOs.MemberProfile;
using Flexfit.Models;
using Flexfit.Repositories;
using System;
using System.Threading.Tasks;

namespace Flexfit.Service
{
    public class MemberProfileService : IMemberProfileService
    {
        private readonly IMemberProfileRepository _profileRepo;

        public MemberProfileService(IMemberProfileRepository profileRepo)
        {
            _profileRepo = profileRepo;
        }

        public async Task<MemberProfileResponse> GetProfileByUserIdAsync(Guid userId)
        {
            var profile = await _profileRepo.GetByUserIdAsync(userId);

            if (profile == null)
            {
                var user = await _profileRepo.GetUserByIdAsync(userId);
                if (user == null) throw new Exception("Tài khoản người dùng không tồn tại.");

                return new MemberProfileResponse
                {
                    UserId = user.UserId,
                    FullName = user.FullName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    DateOfBirth = user.DateOfBirth // Đồng bộ kiểu DateOnly?
                };
            }

            return MapToResponse(profile);
        }

        public async Task<MemberProfileResponse> UpsertProfileAsync(Guid userId, UpdateMemberProfileRequest request)
        {
            var user = await _profileRepo.GetUserByIdAsync(userId);
            if (user == null) throw new Exception("Tài khoản người dùng không tồn tại.");

            // 1. Cập nhật thông tin cơ bản ở bảng User
            user.FullName = request.FullName;
            user.PhoneNumber = request.PhoneNumber;
            user.DateOfBirth = request.DateOfBirth; // Đồng bộ kiểu DateOnly?

            await _profileRepo.UpdateUserAsync(user);

            // 2. Xử lý bảng MemberProfile
            var profile = await _profileRepo.GetByUserIdAsync(userId);
            bool isNew = false;

            if (profile == null)
            {
                isNew = true;
                profile = new MemberProfile
                {
                    MemberProfileId = Guid.NewGuid(),
                    UserId = userId
                };
            }

            profile.Gender = request.Gender;
            profile.HeightCm = request.HeightCm;
            profile.WeightKg = request.WeightKg;
            profile.FitnessGoal = request.FitnessGoal;
            profile.ActivityLevel = request.ActivityLevel;
            profile.PreferredWorkoutTime = request.PreferredWorkoutTime;
            profile.Bio = request.Bio;

            if (isNew)
                await _profileRepo.AddProfileAsync(profile);
            else
                await _profileRepo.UpdateProfileAsync(profile);

            await _profileRepo.SaveChangesAsync();

            var updatedProfile = await _profileRepo.GetByUserIdAsync(userId);
            return MapToResponse(updatedProfile ?? profile);
        }

        private MemberProfileResponse MapToResponse(MemberProfile profile)
        {
            return new MemberProfileResponse
            {
                MemberProfileId = profile.MemberProfileId,
                UserId = profile.UserId,
                FullName = profile.User?.FullName ?? "",
                Email = profile.User?.Email ?? "",
                PhoneNumber = profile.User?.PhoneNumber,
                DateOfBirth = profile.User?.DateOfBirth, // Đồng bộ kiểu DateOnly?
                Gender = profile.Gender,
                HeightCm = profile.HeightCm,
                WeightKg = profile.WeightKg,
                FitnessGoal = profile.FitnessGoal,
                ActivityLevel = profile.ActivityLevel,
                PreferredWorkoutTime = profile.PreferredWorkoutTime,
                Bio = profile.Bio
            };
        }
    }
}