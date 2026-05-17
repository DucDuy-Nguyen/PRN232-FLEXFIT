using Flexfit.DTOs.Booking;
using Flexfit.Models;
using Flexfit.Repositories;

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

        // --- Gym Session Booking ---

        public async Task<GymBookingResponse> BookGymSessionAsync(Guid userId, CreateGymBookingRequest request)
        {
            if (request.StartTime <= DateTime.UtcNow)
                throw new Exception("Không thể đặt lịch cho thời gian trong quá khứ.");
            
            if (request.EndTime <= request.StartTime)
                throw new Exception("Thời gian kết thúc phải sau thời gian bắt đầu.");

            var session = await _bookingRepo.GetGymSessionByDetailsAsync(request.BranchId, request.SessionName, request.StartTime, request.EndTime);
            
            if (session == null)
            {
                // Auto-create session if it doesn't exist
                session = new GymSession
                {
                    SessionId = Guid.NewGuid(),
                    BranchId = request.BranchId,
                    SessionName = request.SessionName,
                    StartTime = request.StartTime,
                    EndTime = request.EndTime,
                    Capacity = 100, // Default capacity
                    CreditCost = 0, // Default cost
                    Status = "Active",
                    CreatedAt = DateTime.UtcNow
                };
                await _bookingRepo.AddGymSessionAsync(session);
            }

            var currentBookingsCount = await _bookingRepo.CountGymBookingsBySessionIdAsync(session.SessionId);
            if (currentBookingsCount >= session.Capacity)
                throw new Exception("Session này đã hết chỗ.");

            // TODO: Trừ User Credit nếu có hệ thống tín dụng (UserCredits)

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
            await _bookingRepo.SaveChangesAsync();

            return new GymBookingResponse
            {
                BookingId = booking.BookingId,
                SessionId = booking.SessionId,
                BookingCode = booking.BookingCode,
                CheckInStatus = booking.CheckInStatus,
                Status = booking.Status,
                BookedAt = booking.BookedAt
            };
        }

        public async Task<IEnumerable<GymBookingResponse>> GetMyGymBookingsAsync(Guid userId)
        {
            var bookings = await _bookingRepo.GetGymBookingsByUserIdAsync(userId);
            return bookings.Select(b => new GymBookingResponse
            {
                BookingId = b.BookingId,
                SessionId = b.SessionId,
                SessionName = b.Session.SessionName,
                BranchName = b.Session.Branch?.BranchName,
                GymName = b.Session.Branch?.Gym?.GymName,
                StartTime = b.Session.StartTime,
                EndTime = b.Session.EndTime,
                BookingCode = b.BookingCode,
                CheckInStatus = b.CheckInStatus,
                Status = b.Status,
                CreditUsed = b.CreditUsed,
                BookedAt = b.BookedAt
            });
        }

        public async Task<bool> CancelGymBookingAsync(Guid userId, Guid bookingId)
        {
            var booking = await _bookingRepo.GetGymBookingByIdAsync(bookingId);
            if (booking == null || booking.UserId != userId)
                throw new Exception("Booking không tồn tại hoặc bạn không có quyền huỷ.");

            if (booking.Status == "Cancelled")
                throw new Exception("Booking này đã được huỷ trước đó.");

            if (booking.Session.StartTime <= DateTime.UtcNow)
                throw new Exception("Không thể huỷ khi session đã bắt đầu.");

            booking.Status = "Cancelled";
            booking.CancelledAt = DateTime.UtcNow;
            booking.RefundCredit = booking.CreditUsed; // TODO: Cộng lại credit cho User
            
            await _bookingRepo.UpdateGymBookingAsync(booking);
            await _bookingRepo.SaveChangesAsync();

            return true;
        }

        // --- Class Booking ---

        public async Task<ClassBookingResponse> BookClassAsync(Guid userId, CreateClassBookingRequest request)
        {
            var classObj = await _bookingRepo.GetClassByIdAsync(request.ClassId);
            if (classObj == null) throw new Exception("Class không tồn tại.");

            if (classObj.StartTime <= DateTime.UtcNow)
                throw new Exception("Không thể đặt lịch cho lớp đã bắt đầu hoặc kết thúc.");

            var currentBookingsCount = await _bookingRepo.CountClassBookingsByClassIdAsync(classObj.ClassId);
            if (currentBookingsCount >= classObj.Capacity)
                throw new Exception("Lớp học này đã hết chỗ.");

            // TODO: Trừ User Credit

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
            await _bookingRepo.SaveChangesAsync();

            return new ClassBookingResponse
            {
                BookingId = booking.BookingId,
                ClassId = booking.ClassId,
                BookingCode = booking.BookingCode,
                CheckInStatus = booking.CheckInStatus,
                Status = booking.Status,
                BookedAt = booking.BookedAt
            };
        }

        public async Task<IEnumerable<ClassBookingResponse>> GetMyClassBookingsAsync(Guid userId)
        {
            var bookings = await _bookingRepo.GetClassBookingsByUserIdAsync(userId);
            return bookings.Select(b => new ClassBookingResponse
            {
                BookingId = b.BookingId,
                ClassId = b.ClassId,
                ClassName = b.Class.ClassName,
                CoachName = b.Class.CoachName,
                BranchName = b.Class.Branch?.BranchName,
                GymName = b.Class.Branch?.Gym?.GymName,
                StartTime = b.Class.StartTime,
                EndTime = b.Class.EndTime,
                BookingCode = b.BookingCode,
                CheckInStatus = b.CheckInStatus,
                Status = b.Status,
                CreditUsed = b.CreditUsed,
                BookedAt = b.BookedAt
            });
        }

        public async Task<bool> CancelClassBookingAsync(Guid userId, Guid bookingId)
        {
            var booking = await _bookingRepo.GetClassBookingByIdAsync(bookingId);
            if (booking == null || booking.UserId != userId)
                throw new Exception("Booking không tồn tại hoặc bạn không có quyền huỷ.");

            if (booking.Status == "Cancelled")
                throw new Exception("Booking này đã được huỷ trước đó.");

            if (booking.Class.StartTime <= DateTime.UtcNow)
                throw new Exception("Không thể huỷ khi lớp học đã bắt đầu.");

            booking.Status = "Cancelled";
            booking.CancelledAt = DateTime.UtcNow;
            booking.RefundCredit = booking.CreditUsed; // TODO: Cộng lại credit cho User
            
            await _bookingRepo.UpdateClassBookingAsync(booking);
            await _bookingRepo.SaveChangesAsync();

            return true;
        }

        public async Task<IEnumerable<GymBookingResponse>> GetPartnerGymBookingsAsync(Guid ownerId)
        {
            var bookings = await _bookingRepo.GetGymBookingsByOwnerIdAsync(ownerId);
            return bookings.Select(b => new GymBookingResponse
            {
                BookingId = b.BookingId,
                SessionId = b.SessionId,
                SessionName = b.Session.SessionName,
                BranchName = b.Session.Branch?.BranchName,
                GymName = b.Session.Branch?.Gym?.GymName,
                StartTime = b.Session.StartTime,
                EndTime = b.Session.EndTime,
                BookingCode = b.BookingCode,
                CheckInStatus = b.CheckInStatus,
                Status = b.Status,
                CreditUsed = b.CreditUsed,
                BookedAt = b.BookedAt
            });
        }

        public async Task<IEnumerable<ClassBookingResponse>> GetPartnerClassBookingsAsync(Guid ownerId)
        {
            var bookings = await _bookingRepo.GetClassBookingsByOwnerIdAsync(ownerId);
            return bookings.Select(b => new ClassBookingResponse
            {
                BookingId = b.BookingId,
                ClassId = b.ClassId,
                ClassName = b.Class.ClassName,
                CoachName = b.Class.CoachName,
                BranchName = b.Class.Branch?.BranchName,
                GymName = b.Class.Branch?.Gym?.GymName,
                StartTime = b.Class.StartTime,
                EndTime = b.Class.EndTime,
                BookingCode = b.BookingCode,
                CheckInStatus = b.CheckInStatus,
                Status = b.Status,
                CreditUsed = b.CreditUsed,
                BookedAt = b.BookedAt
            });
        }
    }
}
