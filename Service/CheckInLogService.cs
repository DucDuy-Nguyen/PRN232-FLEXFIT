using Flexfit.DTOs.CheckInLog;
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
            // 🚨 Kiểm tra quyền sở hữu hoặc làm việc tại chi nhánh
            var hasPermission = await _checkInRepo.IsStaffOrOwnerForGymBookingAsync(request.GymBookingId, staffId);
            if (!hasPermission)
            {
                throw new UnauthorizedAccessException("Tài khoản của bạn không thuộc chi nhánh hoặc phòng gym quản lý lịch đặt này!");
            }

            var log = new CheckInLog
            {
                CheckInLogId = Guid.NewGuid(),
                UserId = request.UserId,
                GymBookingId = request.GymBookingId, // Trường liên kết FK trong CheckInLog giữ nguyên
                ClassBookingId = null,
                ScannedBy = staffId,
                Status = request.Status,
                Message = request.Message ?? "Check-in lịch tập Gym",
                ScannedAt = DateTime.UtcNow
            };

            await _checkInRepo.AddAsync(log);
            await _checkInRepo.SaveChangesAsync();

            var createdLog = await _checkInRepo.GetByIdAsync(log.CheckInLogId);
            return MapToResponse(createdLog ?? log);
        }

        // --- LUỒNG 2: CHECK-IN LỚP HỌC (CLASS) ---
        public async Task<CheckInLogResponse> CheckInClassAsync(CheckInClassRequest request, Guid staffId)
        {
            // 🚨 Kiểm tra quyền sở hữu hoặc làm việc tại chi nhánh
            var hasPermission = await _checkInRepo.IsStaffOrOwnerForClassBookingAsync(request.ClassBookingId, staffId);
            if (!hasPermission)
            {
                throw new UnauthorizedAccessException("Tài khoản của bạn không có quyền quét mã tại lớp học thuộc cơ sở này!");
            }

            var log = new CheckInLog
            {
                CheckInLogId = Guid.NewGuid(),
                UserId = request.UserId,
                GymBookingId = null,
                ClassBookingId = request.ClassBookingId, // Trường liên kết FK trong CheckInLog giữ nguyên
                ScannedBy = staffId,
                Status = request.Status,
                Message = request.Message ?? "Check-in lịch học lớp Class",
                ScannedAt = DateTime.UtcNow
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
        public async Task<IEnumerable<CheckInLogResponse>> GetManagedLogsAsync(Guid currentUserId, string role)
        {
            IEnumerable<CheckInLog> logs;

            // Nếu người dùng đăng nhập là hệ thống Admin, cho phép quét và xem toàn bộ dữ liệu hệ thống
            if (role == "Admin")
            {
                logs = await _checkInRepo.GetAllAsync();
            }
            // Nếu là Đối tác (GymPartner) hoặc Nhân viên (Staff), tiến hành lọc nghiêm ngặt theo phân hệ cơ sở phụ trách
            else
            {
                logs = await _checkInRepo.GetLogsForManagerAsync(currentUserId);
            }

            return logs.Select(MapToResponse);
        }
    }
}