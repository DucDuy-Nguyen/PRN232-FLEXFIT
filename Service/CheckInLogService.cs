using Flexfit.DTOs.CheckInLog;
using Flexfit.Helpers;
using Flexfit.Models;
using Flexfit.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Flexfit.Service
{
    public class CheckInLogService : ICheckInLogService
    {
        private readonly ICheckInLogRepository _checkInRepo;

        public CheckInLogService(ICheckInLogRepository checkInRepo)
        {
            _checkInRepo = checkInRepo;
        }

        public async Task<IEnumerable<CheckInLogResponse>> GetAllLogsAsync()
        {
            var logs = await _checkInRepo.GetAllAsync();
            return logs.Select(MapToResponse);
        }

        public async Task<IEnumerable<CheckInLogResponse>> GetLogsByUserIdAsync(Guid userId)
        {
            var logs = await _checkInRepo.GetByUserIdAsync(userId);
            return logs.Select(MapToResponse);
        }

        // --- LUỒNG 1: CHECK-IN PHÒNG GYM TỰ DO ---
        public async Task<CheckInLogResponse> CheckInGymAsync(CheckInGymRequest request, Guid staffId)
        {
            var log = new CheckInLog
            {
                CheckInLogId = Guid.NewGuid(),
                UserId = request.UserId,
                GymBookingId = request.GymBookingId,
                ClassBookingId = null, // Đảm bảo trường Class luôn rỗng
                ScannedBy = staffId,
                Status = request.Status,
                Message = request.Message ?? "Check-in lịch tập Gym",
                ScannedAt = DateTimeHelper.GetVietnamTime()
            };

            await _checkInRepo.AddAsync(log);
            await _checkInRepo.SaveChangesAsync();

            var createdLog = await _checkInRepo.GetByIdAsync(log.CheckInLogId);
            return MapToResponse(createdLog ?? log);
        }

        // --- LUỒNG 2: CHECK-IN LỚP HỌC (CLASS) ---
        public async Task<CheckInLogResponse> CheckInClassAsync(CheckInClassRequest request, Guid staffId)
        {
            var log = new CheckInLog
            {
                CheckInLogId = Guid.NewGuid(),
                UserId = request.UserId,
                GymBookingId = null, // Đảm bảo trường Gym luôn rỗng
                ClassBookingId = request.ClassBookingId,
                ScannedBy = staffId,
                Status = request.Status,
                Message = request.Message ?? "Check-in lịch học lớp Class",
                ScannedAt = DateTimeHelper.GetVietnamTime()
            };

            await _checkInRepo.AddAsync(log);
            await _checkInRepo.SaveChangesAsync();

            var createdLog = await _checkInRepo.GetByIdAsync(log.CheckInLogId);
            return MapToResponse(createdLog ?? log);
        }

        private CheckInLogResponse MapToResponse(CheckInLog log)
        {
            return new CheckInLogResponse
            {
                CheckInLogId = log.CheckInLogId,
                UserId = log.UserId,
                MemberName = log.User?.FullName ?? "N/A",
                MemberEmail = log.User?.Email ?? "N/A",
                GymBookingId = log.GymBookingId,
                ClassBookingId = log.ClassBookingId,
                ClassName = log.ClassBooking?.Class?.ClassName,
                ScannedBy = log.ScannedBy,
                ScannedByName = log.ScannedByNavigation?.FullName ?? "Hệ thống",
                Status = log.Status,
                Message = log.Message,
                ScannedAt = log.ScannedAt
            };
        }
    }
}