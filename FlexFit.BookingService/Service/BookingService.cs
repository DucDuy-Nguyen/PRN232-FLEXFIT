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
    public class BookingService : IBookingService
    {
        private readonly IBookingRepository _bookingRepo;
        private readonly ICatalogServiceClient _catalogClient;

        public BookingService(
            IBookingRepository bookingRepo,
            ICatalogServiceClient catalogClient)
        {
            _bookingRepo = bookingRepo;
            _catalogClient = catalogClient;
        }

        private string GenerateBookingCode()
        {
            return "BK" + new Random().Next(100000, 999999).ToString();
        }

        private Dictionary<string, IEnumerable<CustomerBookingHistoryResponse>> GroupBookingsIntoTabs(
            IEnumerable<CustomerBookingHistoryResponse> bookingsList, DateTime now)
        {
            var active = bookingsList.Where(b => b.Status != "Cancelled" && b.StartTime <= now && b.EndTime >= now).OrderBy(b => b.StartTime);
            var upcoming = bookingsList.Where(b => b.Status != "Cancelled" && b.StartTime > now).OrderBy(b => b.StartTime);
            var past = bookingsList.Where(b => b.Status == "Cancelled" || b.EndTime < now).OrderByDescending(b => b.EndTime);

            return new Dictionary<string, IEnumerable<CustomerBookingHistoryResponse>>
            {
                { "Active", active },
                { "Upcoming", upcoming },
                { "Past", past }
            };
        }

        // ========================================================
        // 1. GYM SESSION BOOKING
        // ========================================================
        public async Task<GymBookingResponse> BookGymSessionAsync(Guid userId, CreateGymBookingRequest request)
        {
            var now = DateTimeHelper.GetVietnamTime();

            if (request.StartTime <= now) throw new Exception("Không thể đặt lịch cho thời gian trong quá khứ.");
            if (request.EndTime <= request.StartTime) throw new Exception("Thời gian kết thúc phải sau thời gian bắt đầu.");

            // Fetch session details from Catalog Service
            var catalogSession = await _catalogClient.GetGymSessionDetailsAsync(request.BranchId); // SessionId maps to BranchId in old API schema
            if (catalogSession == null)
            {
                // Fallback details if not found (matching monolith behavior)
                catalogSession = new CatalogSessionDetails
                {
                    SessionId = Guid.NewGuid(),
                    GymId = Guid.NewGuid(),
                    GymName = "Flexfit Club",
                    BranchId = request.BranchId,
                    BranchName = "Chi nhánh Flexfit",
                    BranchAddress = "Địa chỉ hệ thống",
                    SessionName = request.SessionName,
                    StartTime = request.StartTime,
                    EndTime = request.EndTime,
                    Capacity = 100,
                    CreditCost = 5,
                    Status = "Open"
                };
            }

            // ExistsDuplicate check
            if (await _bookingRepo.ExistsDuplicateGymBookingAsync(userId, catalogSession.SessionId, catalogSession.StartTime))
                throw new Exception("Hội viên đã đặt lịch tập gym cho khung giờ này trong ngày.");

            // Overlap check
            if (await _bookingRepo.HasOverlappingBookingAsync(userId, catalogSession.StartTime, catalogSession.EndTime))
                throw new Exception("Lịch tập trùng khớp với một lịch đặt gym hoặc lớp học khác đã tồn tại.");

            // Capacity check
            int currentCapacity = await _bookingRepo.CountActiveClassBookingsAsync(catalogSession.SessionId); // capacity check
            if (currentCapacity >= catalogSession.Capacity)
                throw new Exception("Session này đã hết chỗ.");

            // Daily limits check (cancellation count must be less than 2)
            if (await _bookingRepo.GetCancellationCountTodayAsync(userId) >= 2)
                throw new Exception("Mỗi ngày hủy tối đa 2 lịch đặt.");

            var booking = new GymBooking
            {
                BookingId = Guid.NewGuid(),
                UserId = userId,
                SessionId = catalogSession.SessionId,
                BranchId = catalogSession.BranchId,
                GymId = catalogSession.GymId,
                BookingCode = GenerateBookingCode(),
                CreditUsed = catalogSession.CreditCost,
                CheckInStatus = "NotCheckedIn",
                Status = "PendingPayment", // Saga will update status to Confirmed
                BookedAt = now,
                CreatedAt = now,
                UpdatedAt = now,

                // Snapshots denormalization
                GymNameSnapshot = catalogSession.GymName,
                SessionNameSnapshot = catalogSession.SessionName,
                BranchNameSnapshot = catalogSession.BranchName,
                BranchAddressSnapshot = catalogSession.BranchAddress,
                StartTimeSnapshot = catalogSession.StartTime,
                EndTimeSnapshot = catalogSession.EndTime
            };

            await _bookingRepo.AddGymBookingAsync(booking);

            // Construct and enqueue Outbox Message
            var eventPayload = new GymBookingCreatedEvent
            {
                BookingId = booking.BookingId,
                UserId = userId,
                CreditAmount = booking.CreditUsed,
                CorrelationId = booking.BookingId
            };

            var outbox = new OutboxMessage
            {
                OutboxMessageId = Guid.NewGuid(),
                EventType = typeof(GymBookingCreatedEvent).Name,
                AggregateType = "GymBooking",
                AggregateId = booking.BookingId,
                Payload = JsonSerializer.Serialize(eventPayload),
                CorrelationId = booking.BookingId.ToString(),
                OccurredAt = DateTime.UtcNow
            };

            await _bookingRepo.AddOutboxMessageAsync(outbox);
            await _bookingRepo.SaveChangesAsync();

            return new GymBookingResponse
            {
                BookingId = booking.BookingId,
                SessionId = booking.SessionId,
                SessionName = booking.SessionNameSnapshot,
                BranchName = booking.BranchNameSnapshot,
                GymName = booking.GymNameSnapshot,
                StartTime = booking.StartTimeSnapshot,
                EndTime = booking.EndTimeSnapshot,
                BookingCode = booking.BookingCode,
                CheckInStatus = booking.CheckInStatus,
                Status = booking.Status,
                CreditUsed = booking.CreditUsed,
                BookedAt = booking.BookedAt
            };
        }

        public async Task<IEnumerable<GymBookingResponse>> GetMyGymBookingsAsync(Guid userId)
        {
            var bookings = await _bookingRepo.GetUserGymBookingsAsync(userId);
            return bookings.Select(b => new GymBookingResponse
            {
                BookingId = b.BookingId,
                SessionId = b.SessionId,
                SessionName = b.SessionNameSnapshot,
                BranchName = b.BranchNameSnapshot,
                GymName = b.GymNameSnapshot,
                StartTime = b.StartTimeSnapshot,
                EndTime = b.EndTimeSnapshot,
                BookingCode = b.BookingCode,
                CheckInStatus = b.CheckInStatus,
                Status = b.Status,
                CreditUsed = b.CreditUsed,
                BookedAt = b.BookedAt
            });
        }

        public async Task<GymBookingResponse> CancelGymBookingAsync(Guid userId, Guid bookingId)
        {
            var booking = await _bookingRepo.GetGymBookingByIdAsync(bookingId);
            if (booking == null || booking.UserId != userId) throw new Exception("Booking không tồn tại hoặc lỗi quyền.");
            if (booking.Status == "Cancelled") throw new Exception("Booking này đã được huỷ trước đó.");

            var now = DateTimeHelper.GetVietnamTime();
            if (booking.StartTimeSnapshot <= now) throw new Exception("Không thể huỷ khi session đã bắt đầu.");
            if (await _bookingRepo.GetCancellationCountTodayAsync(userId) >= 2) throw new Exception("Mỗi ngày hủy tối đa 2 lịch đặt.");

            double refundPercentage = 0;
            var timeRemaining = booking.StartTimeSnapshot - now;
            if (timeRemaining.TotalHours >= 12) refundPercentage = 100;
            else if (timeRemaining.TotalHours >= 6) refundPercentage = 70;
            else if (timeRemaining.TotalHours >= 3) refundPercentage = 50;
            else if (timeRemaining.TotalHours >= 1) refundPercentage = 25;

            int refundAmount = (int)Math.Round(booking.CreditUsed * (refundPercentage / 100.0));
            booking.Status = "Cancelled";
            booking.CancelledAt = now;
            booking.RefundCredit = refundAmount;
            booking.UpdatedAt = now;

            await _bookingRepo.UpdateGymBookingAsync(booking);

            // Construct and enqueue Outbox Message
            var eventPayload = new BookingCancelledEvent
            {
                BookingId = booking.BookingId,
                BookingType = "GYM",
                UserId = userId,
                CreditAmount = refundAmount,
                CorrelationId = booking.BookingId
            };

            var outbox = new OutboxMessage
            {
                OutboxMessageId = Guid.NewGuid(),
                EventType = typeof(BookingCancelledEvent).Name,
                AggregateType = "GymBooking",
                AggregateId = booking.BookingId,
                Payload = JsonSerializer.Serialize(eventPayload),
                CorrelationId = booking.BookingId.ToString(),
                OccurredAt = DateTime.UtcNow
            };

            await _bookingRepo.AddOutboxMessageAsync(outbox);
            await _bookingRepo.SaveChangesAsync();

            return new GymBookingResponse
            {
                BookingId = booking.BookingId,
                SessionId = booking.SessionId,
                SessionName = booking.SessionNameSnapshot,
                BranchName = booking.BranchNameSnapshot,
                GymName = booking.GymNameSnapshot,
                StartTime = booking.StartTimeSnapshot,
                EndTime = booking.EndTimeSnapshot,
                BookingCode = booking.BookingCode,
                CheckInStatus = booking.CheckInStatus,
                Status = booking.Status,
                CreditUsed = booking.CreditUsed,
                BookedAt = booking.BookedAt
            };
        }

        // ========================================================
        // 2. CLASS BOOKING
        // ========================================================
        public async Task<ClassBookingResponse> BookClassAsync(Guid userId, CreateClassBookingRequest request)
        {
            var now = DateTimeHelper.GetVietnamTime();
            var classObj = await _catalogClient.GetClassDetailsAsync(request.ClassId);
            if (classObj == null) throw new Exception("Class không tồn tại.");
            if (classObj.StartTime <= now) throw new Exception("Không thể đặt lịch cho lớp đã bắt đầu hoặc kết thúc.");

            // ExistsDuplicate check
            if (await _bookingRepo.ExistsDuplicateClassBookingAsync(userId, classObj.ClassId, classObj.StartTime))
                throw new Exception("Hội viên đã đặt lịch lớp học này trong ngày.");

            // Overlap check
            if (await _bookingRepo.HasOverlappingBookingAsync(userId, classObj.StartTime, classObj.EndTime))
                throw new Exception("Lịch tập trùng khớp với một lịch đặt gym hoặc lớp học khác đã tồn tại.");

            // Capacity check
            int currentCapacity = await _bookingRepo.CountActiveClassBookingsAsync(classObj.ClassId);
            if (currentCapacity >= classObj.Capacity)
                throw new Exception("Lớp học này đã hết chỗ.");

            // Cancellation limit check
            if (await _bookingRepo.GetCancellationCountTodayAsync(userId) >= 2)
                throw new Exception("Mỗi ngày hủy tối đa 2 lịch đặt.");

            var booking = new ClassBooking
            {
                BookingId = Guid.NewGuid(),
                UserId = userId,
                ClassId = classObj.ClassId,
                ScheduleId = classObj.ScheduleId,
                BranchId = classObj.BranchId,
                GymId = classObj.GymId,
                BookingCode = GenerateBookingCode(),
                CreditUsed = classObj.CreditCost,
                CheckInStatus = "NotCheckedIn",
                Status = "PendingPayment", // Saga will update status to Confirmed
                BookedAt = now,
                CreatedAt = now,
                UpdatedAt = now,

                // Snapshots denormalization
                GymNameSnapshot = classObj.GymName,
                ClassNameSnapshot = classObj.ClassName,
                BranchNameSnapshot = classObj.BranchName,
                BranchAddressSnapshot = classObj.BranchAddress,
                CoachNameSnapshot = classObj.CoachName,
                StartTimeSnapshot = classObj.StartTime,
                EndTimeSnapshot = classObj.EndTime
            };

            await _bookingRepo.AddClassBookingAsync(booking);

            // Construct and enqueue Outbox Message
            var eventPayload = new ClassBookingCreatedEvent
            {
                BookingId = booking.BookingId,
                UserId = userId,
                CreditAmount = booking.CreditUsed,
                CorrelationId = booking.BookingId
            };

            var outbox = new OutboxMessage
            {
                OutboxMessageId = Guid.NewGuid(),
                EventType = typeof(ClassBookingCreatedEvent).Name,
                AggregateType = "ClassBooking",
                AggregateId = booking.BookingId,
                Payload = JsonSerializer.Serialize(eventPayload),
                CorrelationId = booking.BookingId.ToString(),
                OccurredAt = DateTime.UtcNow
            };

            await _bookingRepo.AddOutboxMessageAsync(outbox);
            await _bookingRepo.SaveChangesAsync();

            return new ClassBookingResponse
            {
                BookingId = booking.BookingId,
                ClassId = booking.ClassId,
                ClassName = booking.ClassNameSnapshot,
                CoachName = booking.CoachNameSnapshot,
                BranchName = booking.BranchNameSnapshot,
                GymName = booking.GymNameSnapshot,
                StartTime = booking.StartTimeSnapshot,
                EndTime = booking.EndTimeSnapshot,
                BookingCode = booking.BookingCode,
                CheckInStatus = booking.CheckInStatus,
                Status = booking.Status,
                CreditUsed = booking.CreditUsed,
                BookedAt = booking.BookedAt
            };
        }

        public async Task<IEnumerable<ClassBookingResponse>> GetMyClassBookingsAsync(Guid userId)
        {
            var bookings = await _bookingRepo.GetUserClassBookingsAsync(userId);
            return bookings.Select(b => new ClassBookingResponse
            {
                BookingId = b.BookingId,
                ClassId = b.ClassId,
                ClassName = b.ClassNameSnapshot,
                CoachName = b.CoachNameSnapshot,
                BranchName = b.BranchNameSnapshot,
                GymName = b.GymNameSnapshot,
                StartTime = b.StartTimeSnapshot,
                EndTime = b.EndTimeSnapshot,
                BookingCode = b.BookingCode,
                CheckInStatus = b.CheckInStatus,
                Status = b.Status,
                CreditUsed = b.CreditUsed,
                BookedAt = b.BookedAt
            });
        }

        public async Task<ClassBookingResponse> CancelClassBookingAsync(Guid userId, Guid bookingId)
        {
            var booking = await _bookingRepo.GetClassBookingByIdAsync(bookingId);
            if (booking == null || booking.UserId != userId) throw new Exception("Booking không tồn tại hoặc lỗi quyền.");
            if (booking.Status == "Cancelled") throw new Exception("Booking này đã được huỷ trước đó.");

            var now = DateTimeHelper.GetVietnamTime();
            if (booking.StartTimeSnapshot <= now) throw new Exception("Không thể huỷ khi lớp học đã bắt đầu.");
            if (await _bookingRepo.GetCancellationCountTodayAsync(userId) >= 2) throw new Exception("Mỗi ngày hủy tối đa 2 lịch đặt.");

            double refundPercentage = 0;
            var timeRemaining = booking.StartTimeSnapshot - now;
            if (timeRemaining.TotalHours >= 12) refundPercentage = 100;
            else if (timeRemaining.TotalHours >= 6) refundPercentage = 70;
            else if (timeRemaining.TotalHours >= 3) refundPercentage = 50;
            else if (timeRemaining.TotalHours >= 1) refundPercentage = 25;

            int refundAmount = (int)Math.Round(booking.CreditUsed * (refundPercentage / 100.0));
            booking.Status = "Cancelled";
            booking.CancelledAt = now;
            booking.RefundCredit = refundAmount;
            booking.UpdatedAt = now;

            await _bookingRepo.UpdateClassBookingAsync(booking);

            // Construct and enqueue Outbox Message
            var eventPayload = new BookingCancelledEvent
            {
                BookingId = booking.BookingId,
                BookingType = "CLASS",
                UserId = userId,
                CreditAmount = refundAmount,
                CorrelationId = booking.BookingId
            };

            var outbox = new OutboxMessage
            {
                OutboxMessageId = Guid.NewGuid(),
                EventType = typeof(BookingCancelledEvent).Name,
                AggregateType = "ClassBooking",
                AggregateId = booking.BookingId,
                Payload = JsonSerializer.Serialize(eventPayload),
                CorrelationId = booking.BookingId.ToString(),
                OccurredAt = DateTime.UtcNow
            };

            await _bookingRepo.AddOutboxMessageAsync(outbox);
            await _bookingRepo.SaveChangesAsync();

            return new ClassBookingResponse
            {
                BookingId = booking.BookingId,
                ClassId = booking.ClassId,
                ClassName = booking.ClassNameSnapshot,
                CoachName = booking.CoachNameSnapshot,
                BranchName = booking.BranchNameSnapshot,
                GymName = booking.GymNameSnapshot,
                StartTime = booking.StartTimeSnapshot,
                EndTime = booking.EndTimeSnapshot,
                BookingCode = booking.BookingCode,
                CheckInStatus = booking.CheckInStatus,
                Status = booking.Status,
                CreditUsed = booking.CreditUsed,
                BookedAt = booking.BookedAt
            };
        }

        // ========================================================
        // 3. PARTNER METHODS
        // ========================================================
        public async Task<Dictionary<string, IEnumerable<CustomerBookingHistoryResponse>>> GetPartnerGymBookingTabsAsync(Guid ownerId)
        {
            var now = DateTimeHelper.GetVietnamTime();
            var branchIds = await _catalogClient.GetManagedBranchIdsAsync(ownerId, "Partner");
            var gymBookings = await _bookingRepo.GetGymBookingsByBranchIdsAsync(branchIds);

            var mappedList = gymBookings.Select(gb => new CustomerBookingHistoryResponse
            {
                BookingId = gb.BookingId,
                BookingCode = gb.BookingCode,
                BookingType = "GYM",
                Name = gb.SessionNameSnapshot,
                BranchName = gb.BranchNameSnapshot,
                StartTime = gb.StartTimeSnapshot,
                EndTime = gb.EndTimeSnapshot,
                CreditUsed = gb.CreditUsed,
                Status = gb.Status,
                CheckInStatus = gb.CheckInStatus,
                CheckInTime = gb.CheckInTime,
                CustomerName = "Hội viên Flexfit",
                CustomerEmail = ""
            }).ToList();

            return GroupBookingsIntoTabs(mappedList, now);
        }

        public async Task<Dictionary<string, IEnumerable<CustomerBookingHistoryResponse>>> GetPartnerClassBookingTabsAsync(Guid ownerId)
        {
            var now = DateTimeHelper.GetVietnamTime();
            var branchIds = await _catalogClient.GetManagedBranchIdsAsync(ownerId, "Partner");
            var classBookings = await _bookingRepo.GetClassBookingsByBranchIdsAsync(branchIds);

            var mappedList = classBookings.Select(cb => new CustomerBookingHistoryResponse
            {
                BookingId = cb.BookingId,
                BookingCode = cb.BookingCode,
                BookingType = "CLASS",
                Name = cb.ClassNameSnapshot,
                BranchName = cb.BranchNameSnapshot,
                StartTime = cb.StartTimeSnapshot,
                EndTime = cb.EndTimeSnapshot,
                CreditUsed = cb.CreditUsed,
                Status = cb.Status,
                CheckInStatus = cb.CheckInStatus,
                CheckInTime = cb.CheckInTime,
                CustomerName = "Hội viên Flexfit",
                CustomerEmail = ""
            }).ToList();

            return GroupBookingsIntoTabs(mappedList, now);
        }

        public async Task<IEnumerable<StaffCheckInBookingResponse>> GetStaffCheckInBookingsAsync(Guid staffId, string role)
        {
            var branchIds = await _catalogClient.GetManagedBranchIdsAsync(staffId, role);
            var gymBookings = await _bookingRepo.GetGymBookingsByBranchIdsAsync(branchIds);
            var classBookings = await _bookingRepo.GetClassBookingsByBranchIdsAsync(branchIds);

            var gymResponses = gymBookings.Select(gb => new StaffCheckInBookingResponse
            {
                BookingId = gb.BookingId,
                BookingCode = gb.BookingCode,
                BookingType = "GYM",
                UserId = gb.UserId,
                UserEmail = "",
                UserFullName = "Hội viên Flexfit",
                SessionId = gb.SessionId,
                SessionName = gb.SessionNameSnapshot,
                BranchId = gb.BranchId,
                BranchName = gb.BranchNameSnapshot,
                GymName = gb.GymNameSnapshot,
                StartTime = gb.StartTimeSnapshot,
                EndTime = gb.EndTimeSnapshot,
                Status = gb.Status,
                CheckInStatus = gb.CheckInStatus,
                CreditUsed = gb.CreditUsed,
                BookedAt = gb.BookedAt,
                QrToken = gb.QrToken
            });

            var classResponses = classBookings.Select(cb => new StaffCheckInBookingResponse
            {
                BookingId = cb.BookingId,
                ClassId = cb.ClassId,
                BookingCode = cb.BookingCode,
                BookingType = "CLASS",
                UserId = cb.UserId,
                UserEmail = "",
                UserFullName = "Hội viên Flexfit",
                ClassName = cb.ClassNameSnapshot,
                CoachName = cb.CoachNameSnapshot,
                BranchId = cb.BranchId,
                BranchName = cb.BranchNameSnapshot,
                GymName = cb.GymNameSnapshot,
                StartTime = cb.StartTimeSnapshot,
                EndTime = cb.EndTimeSnapshot,
                Status = cb.Status,
                CheckInStatus = cb.CheckInStatus,
                CreditUsed = cb.CreditUsed,
                BookedAt = cb.BookedAt,
                QrToken = cb.QrToken
            });

            return gymResponses.Concat(classResponses).OrderByDescending(b => b.BookedAt).ToList();
        }
    }
}
