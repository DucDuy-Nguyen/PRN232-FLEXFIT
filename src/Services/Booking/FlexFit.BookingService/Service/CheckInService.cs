using FlexFit.BookingService.DTOs.Requests;
using FlexFit.BookingService.DTOs.Responses;
using FlexFit.BookingService.ExternalServices.Catalog;
using FlexFit.BookingService.Helpers;
using FlexFit.BookingService.Messaging.Events;
using FlexFit.BookingService.Models;
using FlexFit.BookingService.Repositories.Interfaces;
using FlexFit.BookingService.Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace FlexFit.BookingService.Service
{
    public class CheckInService : ICheckInService
    {
        private readonly ICheckInRepository _checkInRepo;
        private readonly ICatalogServiceClient _catalogClient;

        public CheckInService(
            ICheckInRepository checkInRepo,
            ICatalogServiceClient catalogClient)
        {
            _checkInRepo = checkInRepo;
            _catalogClient = catalogClient;
        }

        private CheckInLogResponse MapToResponse(CheckInLog log)
        {
            return new CheckInLogResponse
            {
                CheckInLogId = log.CheckInLogId,
                UserId = log.UserId,
                MemberName = "Hội viên Flexfit",
                MemberEmail = "",
                GymBookingId = log.GymBookingId,
                ClassBookingId = log.ClassBookingId,
                ClassName = log.ClassBooking?.ClassNameSnapshot ?? log.GymBooking?.SessionNameSnapshot,
                ScannedBy = log.ScannedBy,
                ScannedByName = "Nhân viên hệ thống",
                Status = log.Status,
                Message = log.Message,
                ScannedAt = log.ScannedAt
            };
        }

        public async Task<IEnumerable<CheckInLogResponse>> GetAllLogsAsync()
        {
            var logs = await _checkInRepo.GetCheckInHistoryAsync(Guid.Empty); // Fallback get all or search
            // If Guid.Empty, get all check in logs on branch
            return logs.Select(MapToResponse);
        }

        public async Task<IEnumerable<CheckInLogResponse>> GetLogsByUserIdAsync(Guid userId)
        {
            var logs = await _checkInRepo.GetCheckInHistoryAsync(userId);
            return logs.Select(MapToResponse);
        }

        // --- LUONG 1: CHECK-IN GYM TU DO ---
        public async Task<CheckInLogResponse> CheckInGymAsync(CheckInGymRequest request, Guid staffId)
        {
            var lookupBookingId = request.BookingId ?? request.GymBookingId;
            if (!lookupBookingId.HasValue)
            {
                throw new ArgumentException("Vui lòng cung cấp mã booking/bookingId.");
            }

            var booking = await _checkInRepo.GetGymBookingForCheckInAsync(lookupBookingId.Value);
            if (booking == null)
            {
                throw new ArgumentException("Lịch đặt phòng Gym không tồn tại.");
            }

            // Real-time REST call to Catalog Service to verify staff permissions
            var hasPermission = await _catalogClient.VerifyStaffPermissionAsync(staffId, booking.BranchId);
            if (!hasPermission)
            {
                throw new UnauthorizedAccessException("Tài khoản của bạn không thuộc chi nhánh quản lý lịch đặt này!");
            }

            var now = DateTimeHelper.GetVietnamTime();

            if (booking.CheckInStatus == "CheckedIn" || booking.Status == "Completed")
            {
                throw new InvalidOperationException("Booking đã được check-in trước đó.");
            }

            // Check timing window: booking start - 15m <= now <= booking end + 10m
            if (now < booking.StartTimeSnapshot.AddMinutes(-15))
            {
                throw new InvalidOperationException("Chưa đến giờ check-in.");
            }

            if (now > booking.EndTimeSnapshot.AddMinutes(10))
            {
                throw new InvalidOperationException("Booking đã hết thời gian check-in.");
            }

            var log = new CheckInLog
            {
                CheckInLogId = Guid.NewGuid(),
                UserId = booking.UserId,
                GymBookingId = booking.BookingId,
                ClassBookingId = null,
                ScannedBy = staffId,
                Status = string.IsNullOrWhiteSpace(request.Status) ? "Success" : request.Status,
                Message = request.Message ?? "Check-in lịch tập Gym",
                ScannedAt = now,
                CreatedAt = now
            };

            await _checkInRepo.AddCheckInLogAsync(log);

            booking.CheckInStatus = "CheckedIn";
            booking.CheckInTime = now;
            booking.CheckedInBy = staffId;
            booking.Status = "Completed";
            booking.UpdatedAt = now;

            // Enqueue CheckInCompleted Event in Outbox
            var eventPayload = new CheckInCompletedEvent
            {
                BookingId = booking.BookingId,
                BookingType = "GYM",
                UserId = booking.UserId,
                CorrelationId = booking.BookingId
            };

            var outbox = new OutboxMessage
            {
                OutboxMessageId = Guid.NewGuid(),
                EventType = typeof(CheckInCompletedEvent).Name,
                AggregateType = "GymBooking",
                AggregateId = booking.BookingId,
                Payload = JsonSerializer.Serialize(eventPayload),
                CorrelationId = booking.BookingId.ToString(),
                OccurredAt = DateTime.UtcNow
            };

            await _checkInRepo.AddOutboxMessageAsync(outbox);
            await _checkInRepo.SaveChangesAsync();

            return MapToResponse(log);
        }

        // --- LUONG 2: CHECK-IN CLASS LOP HOC ---
        public async Task<CheckInLogResponse> CheckInClassAsync(CheckInClassRequest request, Guid staffId)
        {
            var booking = await _checkInRepo.GetClassBookingForCheckInAsync(request.ClassBookingId);
            if (booking == null)
            {
                throw new ArgumentException("Lịch đặt lớp học không tồn tại.");
            }

            // Real-time REST call to Catalog Service to verify staff permissions
            var hasPermission = await _catalogClient.VerifyStaffPermissionAsync(staffId, booking.BranchId);
            if (!hasPermission)
            {
                throw new UnauthorizedAccessException("Tài khoản của bạn không có quyền quét mã tại lớp học thuộc cơ sở này!");
            }

            var now = DateTimeHelper.GetVietnamTime();

            if (booking.CheckInStatus == "CheckedIn" || booking.Status == "Completed")
            {
                throw new InvalidOperationException("Booking này đã được check-in.");
            }

            // Timing check
            if (now < booking.StartTimeSnapshot.AddMinutes(-15))
            {
                throw new InvalidOperationException("Chưa đến giờ check-in.");
            }

            if (now > booking.EndTimeSnapshot.AddMinutes(10))
            {
                throw new InvalidOperationException("Booking đã hết thời gian check-in.");
            }

            var log = new CheckInLog
            {
                CheckInLogId = Guid.NewGuid(),
                UserId = booking.UserId,
                GymBookingId = null,
                ClassBookingId = request.ClassBookingId,
                ScannedBy = staffId,
                Status = request.Status,
                Message = request.Message ?? "Check-in lịch học lớp Class",
                ScannedAt = now,
                CreatedAt = now
            };

            await _checkInRepo.AddCheckInLogAsync(log);

            booking.CheckInStatus = "CheckedIn";
            booking.CheckInTime = now;
            booking.CheckedInBy = staffId;
            booking.Status = "Completed";
            booking.UpdatedAt = now;

            // Enqueue CheckInCompleted Event in Outbox
            var eventPayload = new CheckInCompletedEvent
            {
                BookingId = booking.BookingId,
                BookingType = "CLASS",
                UserId = booking.UserId,
                CorrelationId = booking.BookingId
            };

            var outbox = new OutboxMessage
            {
                OutboxMessageId = Guid.NewGuid(),
                EventType = typeof(CheckInCompletedEvent).Name,
                AggregateType = "ClassBooking",
                AggregateId = booking.BookingId,
                Payload = JsonSerializer.Serialize(eventPayload),
                CorrelationId = booking.BookingId.ToString(),
                OccurredAt = DateTime.UtcNow
            };

            await _checkInRepo.AddOutboxMessageAsync(outbox);
            await _checkInRepo.SaveChangesAsync();

            return MapToResponse(log);
        }

        public async Task<IEnumerable<CheckInLogResponse>> GetManagedLogsAsync(Guid currentUserId, string role)
        {
            IEnumerable<CheckInLog> logs;

            if (role == "Admin")
            {
                logs = await _checkInRepo.GetCheckInHistoryAsync(Guid.Empty);
            }
            else
            {
                var branchIds = await _catalogClient.GetManagedBranchIdsAsync(currentUserId, role);
                logs = await _checkInRepo.GetCheckInHistoryByBranchesAsync(branchIds);
            }

            return logs.Select(MapToResponse);
        }
    }
}
