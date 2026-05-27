using Flexfit.DTOs;
using Flexfit.DTOs.Booking;
using Flexfit.Helpers;
using Flexfit.Models;
using Flexfit.Repositories;
using Flexfit.Repository; // Đảm bảo import namespace chứa IPromotionRepository
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Flexfit.Service
{
    public class BookingService : IBookingService
    {
        private readonly IBookingRepository _bookingRepo;
        private readonly IPromotionRepository _promotionRepo; // THÊM REPOSITORY PROMOTION

        public BookingService(IBookingRepository bookingRepo, IPromotionRepository promotionRepo)
        {
            _bookingRepo = bookingRepo;
            _promotionRepo = promotionRepo;
        }

        private string GenerateBookingCode()
        {
            var random = new Random();
            return "BK" + random.Next(100000, 999999).ToString();
        }

        // ========================================================
        // 3. PARTNER / STAFF METHODS (TÁCH BIỆT GYM & CLASS)
        // ========================================================

        public async Task<Dictionary<string, IEnumerable<CustomerBookingHistoryResponse>>> GetPartnerGymBookingTabsAsync(Guid ownerId)
        {
            var now = DateTimeHelper.GetVietnamTime();
            var gymBookingsList = new List<CustomerBookingHistoryResponse>();

            var gymBookings = await _bookingRepo.GetGymBookingsByOwnerIdAsync(ownerId);
            if (gymBookings != null)
            {
                gymBookingsList.AddRange(gymBookings.Select(gb => new CustomerBookingHistoryResponse
                {
                    BookingId = gb.BookingId,
                    BookingCode = gb.BookingCode,
                    BookingType = "GYM",
                    Name = gb.Session?.SessionName ?? "Tập Gym Tự Do",
                    BranchName = gb.Session?.Branch?.BranchName ?? "Chi nhánh Flexfit",
                    StartTime = gb.Session?.StartTime ?? DateTime.MinValue,
                    EndTime = gb.Session?.EndTime ?? DateTime.MinValue,
                    CreditUsed = gb.CreditUsed,
                    Status = gb.Status,
                    CheckInStatus = gb.CheckInStatus,
                    CheckInTime = gb.CheckInTime,
                    CustomerName = gb.User?.FullName ?? "Hội viên Flexfit",
                    CustomerEmail = gb.User?.Email ?? ""
                }));
            }

            var active = gymBookingsList.Where(b => b.Status != "Cancelled" && b.StartTime <= now && b.EndTime >= now).OrderBy(b => b.StartTime);
            var upcoming = gymBookingsList.Where(b => b.Status != "Cancelled" && b.StartTime > now).OrderBy(b => b.StartTime);
            var past = gymBookingsList.Where(b => b.Status == "Cancelled" || b.EndTime < now).OrderByDescending(b => b.EndTime);

            return new Dictionary<string, IEnumerable<CustomerBookingHistoryResponse>>
            {
                { "Active", active },
                { "Upcoming", upcoming },
                { "Past", past }
            };
        }

        public async Task<Dictionary<string, IEnumerable<CustomerBookingHistoryResponse>>> GetPartnerClassBookingTabsAsync(Guid ownerId)
        {
            var now = DateTimeHelper.GetVietnamTime();
            var classBookingsList = new List<CustomerBookingHistoryResponse>();

            var classBookings = await _bookingRepo.GetClassBookingsByOwnerIdAsync(ownerId);
            if (classBookings != null)
            {
                classBookingsList.AddRange(classBookings.Select(cb => new CustomerBookingHistoryResponse
                {
                    BookingId = cb.BookingId,
                    BookingCode = cb.BookingCode,
                    BookingType = "CLASS",
                    Name = cb.Class?.ClassName ?? "Lớp học thể thao",
                    BranchName = cb.Class?.Branch?.BranchName ?? "Chi nhánh Flexfit",
                    StartTime = cb.Class?.StartTime ?? DateTime.MinValue,
                    EndTime = cb.Class?.EndTime ?? DateTime.MinValue,
                    CreditUsed = cb.CreditUsed,
                    Status = cb.Status,
                    CheckInStatus = cb.CheckInStatus,
                    CheckInTime = cb.CheckInTime,
                    CustomerName = cb.User?.FullName ?? "Hội viên Flexfit",
                    CustomerEmail = cb.User?.Email ?? ""
                }));
            }

            var active = classBookingsList.Where(b => b.Status != "Cancelled" && b.StartTime <= now && b.EndTime >= now).OrderBy(b => b.StartTime);
            var upcoming = classBookingsList.Where(b => b.Status != "Cancelled" && b.StartTime > now).OrderBy(b => b.StartTime);
            var past = classBookingsList.Where(b => b.Status == "Cancelled" || b.EndTime < now).OrderByDescending(b => b.EndTime);

            return new Dictionary<string, IEnumerable<CustomerBookingHistoryResponse>>
            {
                { "Active", active },
                { "Upcoming", upcoming },
                { "Past", past }
            };
        }

        // ========================================================
        // 1. GYM SESSION BOOKING (TÍCH HỢP PROMOTION)
        // ========================================================

        public async Task<GymBookingResponse> BookGymSessionAsync(Guid userId, CreateGymBookingRequest request)
        {
            var now = DateTimeHelper.GetVietnamTime();

            if (request.StartTime <= now)
                throw new Exception("Không thể đặt lịch cho thời gian trong quá khứ.");

            if (request.EndTime <= request.StartTime)
                throw new Exception("Thời gian kết thúc phải sau thời gian bắt đầu.");

            var session = await _bookingRepo.GetGymSessionByDetailsAsync(request.BranchId, request.SessionName, request.StartTime, request.EndTime);

            if (session == null)
            {
                var branch = await _bookingRepo.GetBranchByIdAsync(request.BranchId);
                if (branch == null) throw new Exception("Không tìm thấy chi nhánh.");

                session = new GymSession
                {
                    SessionId = Guid.NewGuid(),
                    BranchId = request.BranchId,
                    SessionName = request.SessionName,
                    StartTime = request.StartTime,
                    EndTime = request.EndTime,
                    Capacity = 100,
                    CreditCost = branch.CreditCost,
                    Status = "Active",
                    CreatedAt = now
                };
                await _bookingRepo.AddGymSessionAsync(session);
            }

            var currentBookingsCount = await _bookingRepo.CountGymBookingsBySessionIdAsync(session.SessionId);
            if (currentBookingsCount >= session.Capacity)
                throw new Exception("Session này đã hết chỗ.");

            // LOGIC TÍNH TOÁN GIẢM GIÁ CREDIT QUA PROMOTION
            int finalCreditCost = session.CreditCost;
            if (request.PromotionId.HasValue)
            {
                var promo = await _promotionRepo.GetByIdAsync(request.PromotionId.Value);
                if (promo == null || !promo.IsActive || promo.StartDate > now || promo.EndDate < now)
                {
                    throw new Exception("Chương trình khuyến mãi không khả dụng hoặc đã hết hạn.");
                }

                if (promo.DiscountPercent.HasValue)
                {
                    // Giảm giá theo phần trăm và làm tròn số credit
                    double discountAmount = session.CreditCost * (promo.DiscountPercent.Value / 100.0);
                    finalCreditCost = session.CreditCost - (int)Math.Round(discountAmount);
                    if (finalCreditCost < 0) finalCreditCost = 0; // Đảm bảo không bị âm credit
                }
            }

            var userCredit = await _bookingRepo.GetUserCreditAsync(userId);
            if (userCredit == null || userCredit.Balance < finalCreditCost)
                throw new Exception($"Tài khoản không đủ credit. Cần {finalCreditCost} credit để đặt lịch.");

            int balanceBefore = userCredit.Balance;
            userCredit.Balance -= finalCreditCost;
            userCredit.TotalSpent += finalCreditCost;
            userCredit.UpdatedAt = now;

            var booking = new GymBooking
            {
                BookingId = Guid.NewGuid(),
                UserId = userId,
                SessionId = session.SessionId,
                BookingCode = GenerateBookingCode(),
                CreditUsed = finalCreditCost, // Lưu số credit thực tế đã trừ sau khi giảm giá
                CheckInStatus = "Pending",
                Status = "Confirmed",
                BookedAt = now
            };

            await _bookingRepo.AddGymBookingAsync(booking);

            var transaction = new CreditTransaction
            {
                TransactionId = Guid.NewGuid(),
                UserId = userId,
                Amount = -finalCreditCost,
                BalanceBefore = balanceBefore,
                BalanceAfter = userCredit.Balance,
                Type = "Booking",
                ReferenceId = booking.BookingId,
                ReferenceType = "GymBooking",
                Description = request.PromotionId.HasValue
                    ? $"Đặt lịch tập Gym (Đã áp dụng giảm giá). Khung giờ: {session.SessionName}"
                    : $"Đặt lịch tập Gym thành công. Khung giờ: {session.SessionName}",
                CreatedAt = now
            };
            await _bookingRepo.AddCreditTransactionAsync(transaction);
            await _bookingRepo.SaveChangesAsync();

            var detailedBooking = await _bookingRepo.GetGymBookingByIdAsync(booking.BookingId);

            return new GymBookingResponse
            {
                BookingId = booking.BookingId,
                SessionId = booking.SessionId,
                SessionName = detailedBooking?.Session?.SessionName ?? request.SessionName,
                BranchName = detailedBooking?.Session?.Branch?.BranchName,
                GymName = detailedBooking?.Session?.Branch?.Gym?.GymName,
                StartTime = detailedBooking?.Session?.StartTime ?? request.StartTime,
                EndTime = detailedBooking?.Session?.EndTime ?? request.EndTime,
                BookingCode = booking.BookingCode,
                CheckInStatus = booking.CheckInStatus,
                Status = booking.Status,
                CreditUsed = booking.CreditUsed,
                BookedAt = booking.BookedAt,
                UserEmail = detailedBooking?.User?.Email ?? "",
                UserFullName = detailedBooking?.User?.FullName ?? "Hội viên Flexfit"
            };
        }

        public async Task<IEnumerable<GymBookingResponse>> GetMyGymBookingsAsync(Guid userId)
        {
            var bookings = await _bookingRepo.GetGymBookingsByUserIdAsync(userId);
            return bookings.Select(b => new GymBookingResponse
            {
                BookingId = b.BookingId,
                SessionId = b.SessionId,
                SessionName = b.Session?.SessionName,
                BranchName = b.Session?.Branch?.BranchName,
                GymName = b.Session?.Branch?.Gym?.GymName,
                StartTime = b.Session?.StartTime ?? DateTime.MinValue,
                EndTime = b.Session?.EndTime ?? DateTime.MinValue,
                BookingCode = b.BookingCode,
                CheckInStatus = b.CheckInStatus,
                Status = b.Status,
                CreditUsed = b.CreditUsed,
                BookedAt = b.BookedAt,
                UserEmail = b.User?.Email ?? "",
                UserFullName = b.User?.FullName ?? ""
            });
        }

        public async Task<GymBookingResponse> CancelGymBookingAsync(Guid userId, Guid bookingId)
        {
            var booking = await _bookingRepo.GetGymBookingByIdAsync(bookingId);
            if (booking == null || booking.UserId != userId)
                throw new Exception("Booking không tồn tại hoặc bạn không có quyền huỷ.");

            if (booking.Status == "Cancelled")
                throw new Exception("Booking này đã được huỷ trước đó.");

            var now = DateTimeHelper.GetVietnamTime();

            if (booking.Session != null && booking.Session.StartTime <= now)
                throw new Exception("Không thể huỷ khi session đã bắt đầu.");

            var todayCancelledCount = await _bookingRepo.GetCancellationCountTodayAsync(userId);
            if (todayCancelledCount >= 2)
                throw new Exception("Mỗi ngày bạn chỉ được hủy tối đa 2 lịch đặt.");

            double refundPercentage = 0;
            if (booking.Session != null)
            {
                var timeRemaining = booking.Session.StartTime - now;
                if (timeRemaining.TotalHours >= 12) refundPercentage = 100;
                else if (timeRemaining.TotalHours >= 6) refundPercentage = 70;
                else if (timeRemaining.TotalHours >= 3) refundPercentage = 50;
                else if (timeRemaining.TotalHours >= 1) refundPercentage = 25;
                else refundPercentage = 0;
            }

            // Hoàn tiền dựa trên lượng `CreditUsed` đã lưu (đã tính giảm giá trước đó nên hoàn cực kỳ chuẩn)
            int refundAmount = (int)Math.Round(booking.CreditUsed * (refundPercentage / 100.0));

            booking.Status = "Cancelled";
            booking.CancelledAt = now;
            booking.RefundCredit = refundAmount;

            if (refundAmount > 0)
            {
                var userCredit = await _bookingRepo.GetUserCreditAsync(userId);
                if (userCredit != null)
                {
                    int balanceBefore = userCredit.Balance;
                    userCredit.Balance += refundAmount;
                    userCredit.TotalSpent = Math.Max(0, userCredit.TotalSpent - refundAmount);
                    userCredit.UpdatedAt = now;

                    var transaction = new CreditTransaction
                    {
                        TransactionId = Guid.NewGuid(),
                        UserId = userId,
                        Amount = refundAmount,
                        BalanceBefore = balanceBefore,
                        BalanceAfter = userCredit.Balance,
                        Type = "Refund",
                        ReferenceId = booking.BookingId,
                        ReferenceType = "GymBooking",
                        Description = $"Hoàn trả credit do hủy lịch tập Gym ({refundPercentage}% hoàn trả). Khung giờ: {booking.Session?.SessionName}",
                        CreatedAt = now
                    };
                    await _bookingRepo.AddCreditTransactionAsync(transaction);
                }
            }

            await _bookingRepo.UpdateGymBookingAsync(booking);
            await _bookingRepo.SaveChangesAsync();

            return new GymBookingResponse
            {
                BookingId = booking.BookingId,
                SessionId = booking.SessionId,
                SessionName = booking.Session?.SessionName,
                BranchName = booking.Session?.Branch?.BranchName,
                GymName = booking.Session?.Branch?.Gym?.GymName,
                StartTime = booking.Session?.StartTime ?? DateTime.MinValue,
                EndTime = booking.Session?.EndTime ?? DateTime.MinValue,
                BookingCode = booking.BookingCode,
                CheckInStatus = booking.CheckInStatus,
                Status = booking.Status,
                CreditUsed = booking.CreditUsed,
                BookedAt = booking.BookedAt,
                UserEmail = booking.User?.Email ?? "",
                UserFullName = booking.User?.FullName ?? ""
            };
        }

        // ========================================================
        // 2. CLASS BOOKING (TÍCH HỢP PROMOTION)
        // ========================================================

        public async Task<ClassBookingResponse> BookClassAsync(Guid userId, CreateClassBookingRequest request)
        {
            var now = DateTimeHelper.GetVietnamTime();
            var classObj = await _bookingRepo.GetClassByIdAsync(request.ClassId);
            if (classObj == null) throw new Exception("Class không tồn tại.");

            if (classObj.StartTime <= now)
                throw new Exception("Không thể đặt lịch cho lớp đã bắt đầu hoặc kết thúc.");

            var currentBookingsCount = await _bookingRepo.CountClassBookingsByClassIdAsync(classObj.ClassId);
            if (currentBookingsCount >= classObj.Capacity)
                throw new Exception("Lớp học này đã hết chỗ.");

            // LOGIC TÍNH TOÁN GIẢM GIÁ CREDIT QUA PROMOTION CHO CLASS
            int finalCreditCost = classObj.CreditCost;
            if (request.PromotionId.HasValue)
            {
                var promo = await _promotionRepo.GetByIdAsync(request.PromotionId.Value);
                if (promo == null || !promo.IsActive || promo.StartDate > now || promo.EndDate < now)
                {
                    throw new Exception("Chương trình khuyến mãi không khả dụng hoặc đã hết hạn.");
                }

                if (promo.DiscountPercent.HasValue)
                {
                    double discountAmount = classObj.CreditCost * (promo.DiscountPercent.Value / 100.0);
                    finalCreditCost = classObj.CreditCost - (int)Math.Round(discountAmount);
                    if (finalCreditCost < 0) finalCreditCost = 0;
                }
            }

            var userCredit = await _bookingRepo.GetUserCreditAsync(userId);
            if (userCredit == null || userCredit.Balance < finalCreditCost)
                throw new Exception($"Tài khoản không đủ credit. Cần {finalCreditCost} credit để đặt lịch lớp học.");

            int balanceBefore = userCredit.Balance;
            userCredit.Balance -= finalCreditCost;
            userCredit.TotalSpent += finalCreditCost;
            userCredit.UpdatedAt = now;

            var booking = new ClassBooking
            {
                BookingId = Guid.NewGuid(),
                UserId = userId,
                ClassId = classObj.ClassId,
                BookingCode = GenerateBookingCode(),
                CreditUsed = finalCreditCost, // Lưu số credit thực tế sau giảm giá
                CheckInStatus = "Pending",
                Status = "Confirmed",
                BookedAt = now
            };

            await _bookingRepo.AddClassBookingAsync(booking);

            var transaction = new CreditTransaction
            {
                TransactionId = Guid.NewGuid(),
                UserId = userId,
                Amount = -finalCreditCost,
                BalanceBefore = balanceBefore,
                BalanceAfter = userCredit.Balance,
                Type = "Booking",
                ReferenceId = booking.BookingId,
                ReferenceType = "ClassBooking",
                Description = request.PromotionId.HasValue
                    ? $"Đặt lịch Class (Đã áp dụng giảm giá). Lớp học: {classObj.ClassName}"
                    : $"Đặt lịch Class thành công. Lớp học: {classObj.ClassName}",
                CreatedAt = now
            };
            await _bookingRepo.AddCreditTransactionAsync(transaction);
            await _bookingRepo.SaveChangesAsync();

            var detailedBooking = await _bookingRepo.GetClassBookingByIdAsync(booking.BookingId);

            return new ClassBookingResponse
            {
                BookingId = booking.BookingId,
                ClassId = booking.ClassId,
                ClassName = detailedBooking?.Class?.ClassName ?? classObj.ClassName,
                CoachName = detailedBooking?.Class?.CoachName,
                BranchName = detailedBooking?.Class?.Branch?.BranchName,
                GymName = detailedBooking?.Class?.Branch?.Gym?.GymName,
                StartTime = detailedBooking?.Class?.StartTime ?? classObj.StartTime,
                EndTime = detailedBooking?.Class?.EndTime ?? classObj.EndTime,
                BookingCode = booking.BookingCode,
                CheckInStatus = booking.CheckInStatus,
                Status = booking.Status,
                CreditUsed = booking.CreditUsed,
                BookedAt = booking.BookedAt,
                UserEmail = detailedBooking?.User?.Email ?? "",
                UserFullName = detailedBooking?.User?.FullName ?? "Hội viên Flexfit"
            };
        }

        public async Task<IEnumerable<ClassBookingResponse>> GetMyClassBookingsAsync(Guid userId)
        {
            var bookings = await _bookingRepo.GetClassBookingsByUserIdAsync(userId);
            return bookings.Select(b => new ClassBookingResponse
            {
                BookingId = b.BookingId,
                ClassId = b.ClassId,
                ClassName = b.Class?.ClassName,
                CoachName = b.Class?.CoachName,
                BranchName = b.Class?.Branch?.BranchName,
                GymName = b.Class?.Branch?.Gym?.GymName,
                StartTime = b.Class?.StartTime ?? DateTime.MinValue,
                EndTime = b.Class?.EndTime ?? DateTime.MinValue,
                BookingCode = b.BookingCode,
                CheckInStatus = b.CheckInStatus,
                Status = b.Status,
                CreditUsed = b.CreditUsed,
                BookedAt = b.BookedAt,
                UserEmail = b.User?.Email ?? "",
                UserFullName = b.User?.FullName ?? ""
            });
        }

        public async Task<ClassBookingResponse> CancelClassBookingAsync(Guid userId, Guid bookingId)
        {
            var booking = await _bookingRepo.GetClassBookingByIdAsync(bookingId);
            if (booking == null || booking.UserId != userId)
                throw new Exception("Booking không tồn tại hoặc bạn không có quyền huỷ.");

            if (booking.Status == "Cancelled")
                throw new Exception("Booking này đã được huỷ trước đó.");

            var now = DateTimeHelper.GetVietnamTime();

            if (booking.Class != null && booking.Class.StartTime <= now)
                throw new Exception("Không thể huỷ khi lớp học đã bắt đầu.");

            var todayCancelledCount = await _bookingRepo.GetCancellationCountTodayAsync(userId);
            if (todayCancelledCount >= 2)
                throw new Exception("Mỗi ngày bạn chỉ được hủy tối đa 2 lịch đặt.");

            double refundPercentage = 0;
            if (booking.Class != null)
            {
                var timeRemaining = booking.Class.StartTime - now;
                if (timeRemaining.TotalHours >= 12) refundPercentage = 100;
                else if (timeRemaining.TotalHours >= 6) refundPercentage = 70;
                else if (timeRemaining.TotalHours >= 3) refundPercentage = 50;
                else if (timeRemaining.TotalHours >= 1) refundPercentage = 25;
                else refundPercentage = 0;
            }

            int refundAmount = (int)Math.Round(booking.CreditUsed * (refundPercentage / 100.0));

            booking.Status = "Cancelled";
            booking.CancelledAt = now;
            booking.RefundCredit = refundAmount;

            if (refundAmount > 0)
            {
                var userCredit = await _bookingRepo.GetUserCreditAsync(userId);
                if (userCredit != null)
                {
                    int balanceBefore = userCredit.Balance;
                    userCredit.Balance += refundAmount;
                    userCredit.TotalSpent = Math.Max(0, userCredit.TotalSpent - refundAmount);
                    userCredit.UpdatedAt = now;

                    var transaction = new CreditTransaction
                    {
                        TransactionId = Guid.NewGuid(),
                        UserId = userId,
                        Amount = refundAmount,
                        BalanceBefore = balanceBefore,
                        BalanceAfter = userCredit.Balance,
                        Type = "Refund",
                        ReferenceId = booking.BookingId,
                        ReferenceType = "ClassBooking",
                        Description = $"Hoàn trả credit do hủy lịch Class ({refundPercentage}% hoàn trả). Lớp học: {booking.Class?.ClassName}",
                        CreatedAt = now
                    };
                    await _bookingRepo.AddCreditTransactionAsync(transaction);
                }
            }

            await _bookingRepo.UpdateClassBookingAsync(booking);
            await _bookingRepo.SaveChangesAsync();

            return new ClassBookingResponse
            {
                BookingId = booking.BookingId,
                ClassId = booking.ClassId,
                ClassName = booking.Class?.ClassName,
                CoachName = booking.Class?.CoachName,
                BranchName = booking.Class?.Branch?.BranchName,
                GymName = booking.Class?.Branch?.Gym?.GymName,
                StartTime = booking.Class?.StartTime ?? DateTime.MinValue,
                EndTime = booking.Class?.EndTime ?? DateTime.MinValue,
                BookingCode = booking.BookingCode,
                CheckInStatus = booking.CheckInStatus,
                Status = booking.Status,
                CreditUsed = booking.CreditUsed,
                BookedAt = booking.BookedAt,
                UserEmail = booking.User?.Email ?? "",
                UserFullName = booking.User?.FullName ?? ""
            };
        }

        // ========================================================
        // 3. PARTNER METHODS
        // ========================================================

        public async Task<IEnumerable<GymBookingResponse>> GetPartnerGymBookingsAsync(Guid ownerId)
        {
            var bookings = await _bookingRepo.GetGymBookingsByOwnerIdAsync(ownerId);
            return bookings.Select(b => new GymBookingResponse
            {
                BookingId = b.BookingId,
                SessionId = b.SessionId,
                SessionName = b.Session?.SessionName,
                BranchName = b.Session?.Branch?.BranchName,
                GymName = b.Session?.Branch?.Gym?.GymName,
                StartTime = b.Session?.StartTime ?? DateTime.MinValue,
                EndTime = b.Session?.EndTime ?? DateTime.MinValue,
                BookingCode = b.BookingCode,
                CheckInStatus = b.CheckInStatus,
                Status = b.Status,
                CreditUsed = b.CreditUsed,
                BookedAt = b.BookedAt,
                UserEmail = b.User?.Email ?? "",
                UserFullName = b.User?.FullName ?? ""
            });
        }

        public async Task<IEnumerable<ClassBookingResponse>> GetPartnerClassBookingsAsync(Guid ownerId)
        {
            var bookings = await _bookingRepo.GetClassBookingsByOwnerIdAsync(ownerId);
            return bookings.Select(b => new ClassBookingResponse
            {
                BookingId = b.BookingId,
                ClassId = b.ClassId,
                ClassName = b.Class?.ClassName,
                CoachName = b.Class?.CoachName,
                BranchName = b.Class?.Branch?.BranchName,
                GymName = b.Class?.Branch?.Gym?.GymName,
                StartTime = b.Class?.StartTime ?? DateTime.MinValue,
                EndTime = b.Class?.EndTime ?? DateTime.MinValue,
                BookingCode = b.BookingCode,
                CheckInStatus = b.CheckInStatus,
                Status = b.Status,
                CreditUsed = b.CreditUsed,
                BookedAt = b.BookedAt,
                UserEmail = b.User?.Email ?? "",
                UserFullName = b.User?.FullName ?? ""
            });
        }
    }
}