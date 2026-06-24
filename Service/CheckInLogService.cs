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
        private readonly INotificationService _notificationService;

        public CheckInLogService(ICheckInLogRepository checkInRepo, IWorkoutHistoryService workoutHistoryService, INotificationService notificationService)
        {
            _checkInRepo = checkInRepo;
            _workoutHistoryService = workoutHistoryService;
            _notificationService = notificationService;
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
        // --- LUONG 1: CHECK-IN PHONG GYM TU DO ---
        public async Task<CheckInLogResponse> CheckInGymAsync(CheckInGymRequest request, Guid staffId)
        {
            var lookupBookingId = request.BookingId ?? request.GymBookingId;
            if (!lookupBookingId.HasValue && string.IsNullOrWhiteSpace(request.BookingCode) && string.IsNullOrWhiteSpace(request.QrToken))
            {
                throw new ArgumentException("Vui long cung cap ma booking, bookingId hoac QR token.");
            }

            var booking = await _checkInRepo.FindGymBookingForCheckInAsync(lookupBookingId, request.BookingCode, request.QrToken);
            if (booking == null)
            {
                throw new ArgumentException("Lich dat phong Gym khong ton tai.");
            }

            var hasPermission = await _checkInRepo.IsStaffOrOwnerForGymBookingAsync(booking.BookingId, staffId);
            if (!hasPermission)
            {
                throw new UnauthorizedAccessException("Tai khoan cua ban khong thuoc chi nhanh quan ly lich dat nay!");
            }

            var now = DateTimeHelper.GetVietnamTime();

            if (booking.CheckInStatus == "CheckedIn" || booking.Status == "Completed")
            {
                throw new InvalidOperationException("Booking đã được check-in trước đó.");
            }

            if (booking.Session == null)
            {
                throw new InvalidOperationException("Lich dat Gym khong co thong tin khung gio.");
            }

            if (now < booking.Session.StartTime.AddMinutes(-15))
            {
                throw new InvalidOperationException("Chua den gio check-in.");
            }

            if (now > booking.Session.EndTime.AddMinutes(10))
            {
                throw new InvalidOperationException("Booking da het thoi gian check-in.");
            }

            var log = new CheckInLog
            {
                CheckInLogId = Guid.NewGuid(),
                UserId = booking.UserId,
                GymBookingId = booking.BookingId,
                ClassBookingId = null,
                ScannedBy = staffId,
                Status = string.IsNullOrWhiteSpace(request.Status) ? "Success" : request.Status,
                Message = request.Message ?? "Check-in lich tap Gym",
                ScannedAt = now
            };

            await _checkInRepo.AddAsync(log);

            booking.CheckInStatus = "CheckedIn";
            booking.CheckInTime = now;
            booking.CheckedInBy = staffId;
            booking.Status = "Completed";
            await _checkInRepo.UpdateGymBookingAsync(booking);

            await _workoutHistoryService.CreateHistoryFromCheckInAsync(booking.UserId, null, booking.BookingId);
            await _checkInRepo.SaveChangesAsync();

            var createdLog = await _checkInRepo.GetByIdAsync(log.CheckInLogId);
            // Push real-time check-in confirmation to member
            try
            {
                await _notificationService.SendAsync(booking.UserId, "Check-in thành công ✅", $"Bạn đã check-in thành công cho lịch Gym: {booking.BookingCode}.", "CheckInSuccess");

                // Notify branch staff/owner via branch group
                var branchId = booking.Session?.BranchId;
                if (branchId != null)
                {
                    await _notificationService.BroadcastToBranchAsync(branchId.Value, "Thành viên đã check-in", $"Khách hàng {createdLog?.User?.FullName ?? "Hội viên"} đã check-in tại chi nhánh.", "StaffNotification");
                }
            }
            catch { }
            return MapToResponse(createdLog ?? log);
        }

        // --- LUONG 2: CHECK-IN LOP HOC (CLASS) ---
        public async Task<CheckInLogResponse> CheckInClassAsync(CheckInClassRequest request, Guid staffId)
        {
            // 1. đŸ¨ Kiá»ƒm tra quyá»n sá»Ÿ há»¯u hoáº·c lĂ m viá»‡c táº¡i chi nhĂ¡nh
            var hasPermission = await _checkInRepo.IsStaffOrOwnerForClassBookingAsync(request.ClassBookingId, staffId);
            if (!hasPermission)
            {
                throw new UnauthorizedAccessException("TĂ i khoáº£n cá»§a báº¡n khĂ´ng cĂ³ quyá»n quĂ©t mĂ£ táº¡i lá»›p há»c thuá»™c cÆ¡ sá»Ÿ nĂ y!");
            }

            // Láº¥y thĂ´ng tin chi tiáº¿t cá»§a ClassBooking tÆ°Æ¡ng á»©ng
            var booking = await _checkInRepo.GetClassBookingByIdAsync(request.ClassBookingId);
            if (booking == null)
            {
                throw new ArgumentException("Lá»‹ch Ä‘áº·t lá»›p há»c khĂ´ng tá»“n táº¡i.");
            }

            // 2. đŸ¨ VALIDATE TRáº NG THĂI VĂ€ THá»œI GIAN CHECK-IN
            var now = DateTimeHelper.GetVietnamTime();

            // Kiá»ƒm tra tráº¡ng thĂ¡i Ä‘Ă£ check-in
            if (booking.CheckInStatus == "CheckedIn" || booking.Status == "Completed")
            {
                throw new InvalidOperationException("Booking nĂ y Ä‘Ă£ Ä‘Æ°á»£c check-in.");
            }

            // Kiá»ƒm tra thá»i gian báº¯t Ä‘áº§u lá»›p há»c (trá» qua thá»±c thá»ƒ Class)
            if (now < booking.Class.StartTime.AddMinutes(-15))
            {
                throw new InvalidOperationException("ChÆ°a Ä‘áº¿n giá» check-in.");
            }

            // Kiá»ƒm tra thá»i gian káº¿t thĂºc lá»›p há»c (trá» qua thá»±c thá»ƒ Class)
            if (now > booking.Class.EndTime.AddMinutes(10))
            {
                throw new InvalidOperationException("Booking Ä‘Ă£ háº¿t thá»i gian check-in.");
            }

            // 3. Tiáº¿n hĂ nh ghi nháº­n Log
            var log = new CheckInLog
            {
                CheckInLogId = Guid.NewGuid(),
                UserId = booking.UserId,

                GymBookingId = null,
                ClassBookingId = request.ClassBookingId,
                ScannedBy = staffId,
                Status = request.Status,
                Message = request.Message ?? "Check-in lá»‹ch há»c lá»›p Class",
                ScannedAt = now
            };

            await _checkInRepo.AddAsync(log);

            // Cáº­p nháº­t tráº¡ng thĂ¡i cá»§a ClassBooking tÆ°Æ¡ng á»©ng thĂ nh CheckedIn vĂ  Completed
            if (booking != null)
            {
                booking.CheckInStatus = "CheckedIn";
                booking.CheckInTime = DateTimeHelper.GetVietnamTime();
                booking.Status = "Completed";
                await _checkInRepo.UpdateClassBookingAsync(booking);

                // Tá»° Äá»˜NG GHI NHáº¬N Lá»CH Sá»¬ Táº¬P LUYá»†N
                await _workoutHistoryService.CreateHistoryFromCheckInAsync(booking.UserId, booking.BookingId, null);

            }


            await _checkInRepo.SaveChangesAsync();

            var createdLog = await _checkInRepo.GetByIdAsync(log.CheckInLogId);
            // Push real-time check-in confirmation to member and staff
            try
            {
                await _notificationService.SendAsync(booking.UserId, "Check-in lớp học thành công ✅", $"Bạn đã check-in cho lớp: {booking.BookingCode}.", "CheckInSuccess");

                var branchId = booking.Class?.BranchId;
                if (branchId != null)
                {
                    await _notificationService.BroadcastToBranchAsync(branchId.Value, "Thành viên đã check-in", $"Khách hàng {createdLog?.User?.FullName ?? "Hội viên"} đã check-in lớp {booking.Class?.ClassName}.", "StaffNotification");
                }
            }
            catch { }
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
                ScannedByName = log.ScannedByNavigation?.FullName ?? "Há»‡ thá»‘ng",
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
