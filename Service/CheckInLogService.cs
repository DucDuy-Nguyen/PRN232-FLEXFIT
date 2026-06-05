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
        private readonly IWorkoutHistoryService _workoutHistoryService;

        public CheckInLogService(ICheckInLogRepository checkInRepo, IWorkoutHistoryService workoutHistoryService)
        {
            _checkInRepo = checkInRepo;
            _workoutHistoryService = workoutHistoryService;
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
            // 1. 🚨 Kiểm tra quyền sở hữu hoặc làm việc tại chi nhánh
            var hasPermission = await _checkInRepo.IsStaffOrOwnerForGymBookingAsync(request.GymBookingId, staffId);
            if (!hasPermission)
            {
                throw new UnauthorizedAccessException("Tài khoản của bạn không thuộc chi nhánh hoặc phòng gym quản lý lịch đặt này!");
            }

            // Lấy thông tin chi tiết của GymBooking tương ứng
            var booking = await _checkInRepo.GetGymBookingByIdAsync(request.GymBookingId);
            if (booking == null)
            {
                throw new ArgumentException("Lịch đặt phòng Gym không tồn tại.");
            }

            // 2. 🚨 VALIDATE TRẠNG THÁI VÀ THỜI GIAN CHECK-IN
            var now = DateTimeHelper.GetVietnamTime();

            // Kiểm tra trạng thái đã check-in
            if (booking.CheckInStatus == "CheckedIn" || booking.Status == "Completed")
            {
                throw new InvalidOperationException("Booking này đã được check-in.");
            }

            // Kiểm tra thời gian bắt đầu tập (trỏ qua thực thể Session)
            if (now < booking.Session.StartTime.AddMinutes(-15))
            {
                throw new InvalidOperationException("Chưa đến giờ check-in.");
            }

            // Kiểm tra thời gian kết thúc tập (trỏ qua thực thể Session)
            if (now > booking.Session.EndTime.AddMinutes(10))
            {
                throw new InvalidOperationException("Booking đã hết thời gian check-in.");
            }

            // 3. Tiến hành ghi nhận Log
            var log = new CheckInLog
            {
                CheckInLogId = Guid.NewGuid(),
                UserId = request.UserId,
                GymBookingId = request.GymBookingId,
                ClassBookingId = null,
                ScannedBy = staffId,
                Status = request.Status,
                Message = request.Message ?? "Check-in lịch tập Gym",
                ScannedAt = now
            };

            await _checkInRepo.AddAsync(log);

            // Cập nhật trạng thái của GymBooking tương ứng thành CheckedIn và Completed
            if (booking != null)
            {
                booking.CheckInStatus = "CheckedIn";
                booking.CheckInTime = DateTimeHelper.GetVietnamTime();
                booking.Status = "Completed";
                await _checkInRepo.UpdateGymBookingAsync(booking);

                // TỰ ĐỘNG GHI NHẬN LỊCH SỬ TẬP LUYỆN
                await _workoutHistoryService.CreateHistoryFromCheckInAsync(request.UserId, null, booking.BookingId);
            }

            await _checkInRepo.SaveChangesAsync();

            var createdLog = await _checkInRepo.GetByIdAsync(log.CheckInLogId);
            return MapToResponse(createdLog ?? log);
        }

        // --- LUỒNG 2: CHECK-IN LỚP HỌC (CLASS) ---
        public async Task<CheckInLogResponse> CheckInClassAsync(CheckInClassRequest request, Guid staffId)
        {
            // 1. 🚨 Kiểm tra quyền sở hữu hoặc làm việc tại chi nhánh
            var hasPermission = await _checkInRepo.IsStaffOrOwnerForClassBookingAsync(request.ClassBookingId, staffId);
            if (!hasPermission)
            {
                throw new UnauthorizedAccessException("Tài khoản của bạn không có quyền quét mã tại lớp học thuộc cơ sở này!");
            }

            // Lấy thông tin chi tiết của ClassBooking tương ứng
            var booking = await _checkInRepo.GetClassBookingByIdAsync(request.ClassBookingId);
            if (booking == null)
            {
                throw new ArgumentException("Lịch đặt lớp học không tồn tại.");
            }

            // 2. 🚨 VALIDATE TRẠNG THÁI VÀ THỜI GIAN CHECK-IN
            var now = DateTimeHelper.GetVietnamTime();

            // Kiểm tra trạng thái đã check-in
            if (booking.CheckInStatus == "CheckedIn" || booking.Status == "Completed")
            {
                throw new InvalidOperationException("Booking này đã được check-in.");
            }

            // Kiểm tra thời gian bắt đầu lớp học (trỏ qua thực thể Class)
            if (now < booking.Class.StartTime.AddMinutes(-15))
            {
                throw new InvalidOperationException("Chưa đến giờ check-in.");
            }

            // Kiểm tra thời gian kết thúc lớp học (trỏ qua thực thể Class)
            if (now > booking.Class.EndTime.AddMinutes(10))
            {
                throw new InvalidOperationException("Booking đã hết thời gian check-in.");
            }

            // 3. Tiến hành ghi nhận Log
            var log = new CheckInLog
            {
                CheckInLogId = Guid.NewGuid(),
                UserId = request.UserId,
                GymBookingId = null,
                ClassBookingId = request.ClassBookingId,
                ScannedBy = staffId,
                Status = request.Status,
                Message = request.Message ?? "Check-in lịch học lớp Class",
                ScannedAt = now
            };

            await _checkInRepo.AddAsync(log);

            // Cập nhật trạng thái của ClassBooking tương ứng thành CheckedIn và Completed
            if (booking != null)
            {
                booking.CheckInStatus = "CheckedIn";
                booking.CheckInTime = DateTimeHelper.GetVietnamTime();
                booking.Status = "Completed";
                await _checkInRepo.UpdateClassBookingAsync(booking);

                // TỰ ĐỘNG GHI NHẬN LỊCH SỬ TẬP LUYỆN
                await _workoutHistoryService.CreateHistoryFromCheckInAsync(request.UserId, booking.BookingId, null);
            }


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

            if (role == "Admin")
            {
                logs = await _checkInRepo.GetAllAsync();
            }
            else
            {
                logs = await _checkInRepo.GetLogsForManagerAsync(currentUserId);
            }

            return logs.Select(MapToResponse);
        }
    }
}