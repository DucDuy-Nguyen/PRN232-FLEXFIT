using Flexfit.DTOs.Booking;
using Flexfit.Models;
using Flexfit.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Flexfit.Service
{
    public class BookingService : IBookingService
    {
        private readonly IBookingRepository _bookingRepo;

        public BookingService(IBookingRepository bookingRepo)
        {
            _bookingRepo = bookingRepo;
        }

        private string GenerateBookingCode()
        {
            var random = new Random();
            return "BK" + random.Next(100000, 999999).ToString();
        }

        // ========================================================
        // 1. GYM SESSION BOOKING
        // ========================================================

        public async Task<GymBookingResponse> BookGymSessionAsync(Guid userId, CreateGymBookingRequest request)
        {
            if (request.StartTime <= DateTime.UtcNow)
                throw new Exception("Không thể đặt lịch cho thời gian trong quá khứ.");

            if (request.EndTime <= request.StartTime)
                throw new Exception("Thời gian kết thúc phải sau thời gian bắt đầu.");

            var session = await _bookingRepo.GetGymSessionByDetailsAsync(request.BranchId, request.SessionName, request.StartTime, request.EndTime);

            if (session == null)
            {
                var branch = await _bookingRepo.GetBranchByIdAsync(request.BranchId);
                if (branch == null)
                    throw new Exception("Không tìm thấy chi nhánh.");

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
                    CreatedAt = DateTime.UtcNow
                };
                await _bookingRepo.AddGymSessionAsync(session);
            }

            var currentBookingsCount = await _bookingRepo.CountGymBookingsBySessionIdAsync(session.SessionId);
            if (currentBookingsCount >= session.Capacity)
                throw new Exception("Session này đã hết chỗ.");

            var userCredit = await _bookingRepo.GetUserCreditAsync(userId);
            if (userCredit == null || userCredit.Balance < session.CreditCost)
                throw new Exception("Tài khoản không đủ credit. Vui lòng nạp thêm credit để đặt lịch.");

            int balanceBefore = userCredit.Balance;
            userCredit.Balance -= session.CreditCost;
            userCredit.TotalSpent += session.CreditCost;
            userCredit.UpdatedAt = DateTime.UtcNow;

            var booking = new GymBooking
            {
                BookingId = Guid.NewGuid(),
                UserId = userId,
                SessionId = session.SessionId,
                BookingCode = GenerateBookingCode(),
                CreditUsed = session.CreditCost,
                CheckInStatus = "Pending",
                Status = "Confirmed",
                BookedAt = DateTime.UtcNow
            };

            await _bookingRepo.AddGymBookingAsync(booking);

            var transaction = new CreditTransaction
            {
                TransactionId = Guid.NewGuid(),
                UserId = userId,
                Amount = -session.CreditCost,
                BalanceBefore = balanceBefore,
                BalanceAfter = userCredit.Balance,
                Type = "Booking",
                ReferenceId = booking.BookingId,
                ReferenceType = "GymBooking",
                Description = $"Đặt lịch tập Gym thành công. Khung giờ: {session.SessionName}",
                CreatedAt = DateTime.UtcNow
            };
            await _bookingRepo.AddCreditTransactionAsync(transaction);
            await _bookingRepo.SaveChangesAsync();

            // ĐÃ SỬA: Lấy thông tin chi tiết qua Repo đã nạp đầy đủ Navigation Properties
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
                // ĐÃ FIX: Đảm bảo bốc dữ liệu thực từ bản ghi liên kết, fallback an toàn nếu Repo chưa kịp Include
                UserEmail = detailedBooking?.User?.Email ?? booking.User?.Email ?? "",
                UserFullName = detailedBooking?.User?.FullName ?? booking.User?.FullName ?? "Hội viên Flexfit"
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

            if (booking.Session != null && booking.Session.StartTime <= DateTime.UtcNow)
                throw new Exception("Không thể huỷ khi session đã bắt đầu.");

            booking.Status = "Cancelled";
            booking.CancelledAt = DateTime.UtcNow;
            booking.RefundCredit = booking.CreditUsed;

            if (booking.CreditUsed > 0)
            {
                var userCredit = await _bookingRepo.GetUserCreditAsync(userId);
                if (userCredit != null)
                {
                    int balanceBefore = userCredit.Balance;
                    userCredit.Balance += booking.CreditUsed;
                    userCredit.TotalSpent = Math.Max(0, userCredit.TotalSpent - booking.CreditUsed);
                    userCredit.UpdatedAt = DateTime.UtcNow;

                    var transaction = new CreditTransaction
                    {
                        TransactionId = Guid.NewGuid(),
                        UserId = userId,
                        Amount = booking.CreditUsed,
                        BalanceBefore = balanceBefore,
                        BalanceAfter = userCredit.Balance,
                        Type = "Refund",
                        ReferenceId = booking.BookingId,
                        ReferenceType = "GymBooking",
                        Description = $"Hoàn trả credit do hủy lịch tập Gym thành công. Khung giờ: {booking.Session?.SessionName}",
                        CreatedAt = DateTime.UtcNow
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
                // ĐÃ FIX: Lấy thông tin định danh chính xác phục vụ gửi mail hủy lịch
                UserEmail = booking.User?.Email ?? "",
                UserFullName = booking.User?.FullName ?? ""
            };
        }

        // ========================================================
        // 2. CLASS BOOKING
        // ========================================================

        public async Task<ClassBookingResponse> BookClassAsync(Guid userId, CreateClassBookingRequest request)
        {
            var classObj = await _bookingRepo.GetClassByIdAsync(request.ClassId);
            if (classObj == null) throw new Exception("Class không tồn tại.");

            if (classObj.StartTime <= DateTime.UtcNow)
                throw new Exception("Không thể đặt lịch cho lớp đã bắt đầu hoặc kết thúc.");

            var currentBookingsCount = await _bookingRepo.CountClassBookingsByClassIdAsync(classObj.ClassId);
            if (currentBookingsCount >= classObj.Capacity)
                throw new Exception("Lớp học này đã hết chỗ.");

            var userCredit = await _bookingRepo.GetUserCreditAsync(userId);
            if (userCredit == null || userCredit.Balance < classObj.CreditCost)
                throw new Exception("Tài khoản không đủ credit. Vui lòng nạp thêm credit để đặt lịch lớp học.");

            int balanceBefore = userCredit.Balance;
            userCredit.Balance -= classObj.CreditCost;
            userCredit.TotalSpent += classObj.CreditCost;
            userCredit.UpdatedAt = DateTime.UtcNow;

            var booking = new ClassBooking
            {
                BookingId = Guid.NewGuid(),
                UserId = userId,
                ClassId = classObj.ClassId,
                BookingCode = GenerateBookingCode(),
                CreditUsed = classObj.CreditCost,
                CheckInStatus = "Pending",
                Status = "Confirmed",
                BookedAt = DateTime.UtcNow
            };

            await _bookingRepo.AddClassBookingAsync(booking);

            var transaction = new CreditTransaction
            {
                TransactionId = Guid.NewGuid(),
                UserId = userId,
                Amount = -classObj.CreditCost,
                BalanceBefore = balanceBefore,
                BalanceAfter = userCredit.Balance,
                Type = "Booking",
                ReferenceId = booking.BookingId,
                ReferenceType = "ClassBooking",
                Description = $"Đặt lịch Class thành công. Lớp học: {classObj.ClassName}",
                CreatedAt = DateTime.UtcNow
            };
            await _bookingRepo.AddCreditTransactionAsync(transaction);
            await _bookingRepo.SaveChangesAsync();

            // ĐÃ SỬA: Đồng bộ hóa nạp dữ liệu chi tiết
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
                // ĐÃ FIX: Rút trích thông tin thực từ hệ thống cơ sở dữ liệu liên kết
                UserEmail = detailedBooking?.User?.Email ?? booking.User?.Email ?? "",
                UserFullName = detailedBooking?.User?.FullName ?? booking.User?.FullName ?? "Hội viên Flexfit"
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

            if (booking.Class != null && booking.Class.StartTime <= DateTime.UtcNow)
                throw new Exception("Không thể huỷ khi lớp học đã bắt đầu.");

            booking.Status = "Cancelled";
            booking.CancelledAt = DateTime.UtcNow;
            booking.RefundCredit = booking.CreditUsed;

            if (booking.CreditUsed > 0)
            {
                var userCredit = await _bookingRepo.GetUserCreditAsync(userId);
                if (userCredit != null)
                {
                    int balanceBefore = userCredit.Balance;
                    userCredit.Balance += booking.CreditUsed;
                    userCredit.TotalSpent = Math.Max(0, userCredit.TotalSpent - booking.CreditUsed);
                    userCredit.UpdatedAt = DateTime.UtcNow;

                    var transaction = new CreditTransaction
                    {
                        TransactionId = Guid.NewGuid(),
                        UserId = userId,
                        Amount = booking.CreditUsed,
                        BalanceBefore = balanceBefore,
                        BalanceAfter = userCredit.Balance,
                        Type = "Refund",
                        ReferenceId = booking.BookingId,
                        ReferenceType = "ClassBooking",
                        Description = $"Hoàn trả credit do hủy lịch Class thành công. Lớp học: {booking.Class?.ClassName}",
                        CreatedAt = DateTime.UtcNow
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
                // ĐÃ FIX: Khôi phục cấu trúc thông tin định danh cho Mail lớp học
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