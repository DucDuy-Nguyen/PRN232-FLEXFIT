using Flexfit.DTOs.Review;
using Flexfit.Helpers;
using Flexfit.Models;
using Flexfit.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Flexfit.Service
{
    public class ReviewService : IReviewService
    {
        private readonly IBookingRepository _bookingRepository;
        private readonly FlexFitDbContext _context;

        public ReviewService(IBookingRepository bookingRepository, FlexFitDbContext context)
        {
            _bookingRepository = bookingRepository;
            _context = context;
        }

        public async Task<ReviewResponse> CreateBookingReviewAsync(Guid userId, CreateReviewRequest request)
        {
            // Validate rating range
            if (request.Rating < 1 || request.Rating > 5)
                throw new ArgumentException("Số sao đánh giá phải nằm trong khoảng từ 1 đến 5.");

            if (request.BookingType == "Class")
            {
                return await CreateClassBookingReviewAsync(userId, request);
            }
            else if (request.BookingType == "Gym")
            {
                return await CreateGymBookingReviewAsync(userId, request);
            }
            else
            {
                throw new ArgumentException("Loại đặt lịch không hợp lệ. Vui lòng chọn 'Class' hoặc 'Gym'.");
            }
        }

        // =============================================
        // Class Booking Review
        // =============================================
        private async Task<ReviewResponse> CreateClassBookingReviewAsync(Guid userId, CreateReviewRequest request)
        {
            // RÀO CẢN 1: Tìm booking và kiểm tra ownership
            var booking = await _bookingRepository.GetClassBookingByIdAsync(request.BookingId);
            if (booking == null || booking.UserId != userId)
                throw new KeyNotFoundException("Không tìm thấy lịch đặt lớp học hoặc lịch đặt không thuộc về bạn.");

            // RÀO CẢN 2: Kiểm tra đã hoàn thành check-in chưa
            if (booking.CheckInStatus != "CheckedIn")
                throw new InvalidOperationException("Bạn chỉ có thể đánh giá lịch đặt sau khi đã hoàn thành quét mã điểm danh (Check-in).");

            // RÀO CẢN 3: Kiểm tra đã đánh giá booking này chưa
            var existingReview = await _context.Reviews
                .FirstOrDefaultAsync(r => r.ClassBookingId == request.BookingId);
            if (existingReview != null)
                throw new InvalidOperationException("Lịch đặt này đã được bạn đánh giá trước đó. Mỗi lịch đặt chỉ được đánh giá 1 lần.");

            // Tạo review mới
            var review = new Review
            {
                ReviewId = Guid.NewGuid(),
                UserId = userId,
                ClassId = booking.ClassId,
                ClassBookingId = booking.BookingId,
                Rating = request.Rating,
                Comment = request.Comment,
                CreatedAt = DateTimeHelper.GetVietnamTime()
            };

            await _context.Reviews.AddAsync(review);
            // Lưu ý: Model Class không có thuộc tính RatingAverage nên chỉ lưu review
            await _context.SaveChangesAsync();

            return MapToResponse(review, booking.User?.FullName ?? "");
        }

        // =============================================
        // Gym Booking Review
        // =============================================
        private async Task<ReviewResponse> CreateGymBookingReviewAsync(Guid userId, CreateReviewRequest request)
        {
            // RÀO CẢN 1: Tìm booking và kiểm tra ownership
            var booking = await _bookingRepository.GetGymBookingByIdAsync(request.BookingId);
            if (booking == null || booking.UserId != userId)
                throw new KeyNotFoundException("Không tìm thấy lịch đặt phòng tập hoặc lịch đặt không thuộc về bạn.");

            // RÀO CẢN 2: Kiểm tra đã hoàn thành check-in chưa
            if (booking.CheckInStatus != "CheckedIn")
                throw new InvalidOperationException("Bạn chỉ có thể đánh giá lịch đặt sau khi đã hoàn thành quét mã điểm danh (Check-in).");

            // RÀO CẢN 3: Kiểm tra đã đánh giá booking này chưa
            var existingReview = await _context.Reviews
                .FirstOrDefaultAsync(r => r.GymBookingId == request.BookingId);
            if (existingReview != null)
                throw new InvalidOperationException("Lịch đặt này đã được bạn đánh giá trước đó. Mỗi lịch đặt chỉ được đánh giá 1 lần.");

            // Tạo review mới
            var gymId = booking.Session?.Branch?.GymId;
            var review = new Review
            {
                ReviewId = Guid.NewGuid(),
                UserId = userId,
                GymId = gymId,
                GymBookingId = booking.BookingId,
                Rating = request.Rating,
                Comment = request.Comment,
                CreatedAt = DateTimeHelper.GetVietnamTime()
            };

            await _context.Reviews.AddAsync(review);

            // Cập nhật điểm đánh giá trung bình của phòng tập
            if (gymId.HasValue)
                await UpdateGymRatingAverageAsync(gymId.Value);

            await _context.SaveChangesAsync();

            return MapToResponse(review, booking.User?.FullName ?? "");
        }

        // =============================================
        // Helpers: Cập nhật Rating trung bình
        // =============================================
        private async Task UpdateGymRatingAverageAsync(Guid gymId)
        {
            var gym = await _context.Gyms.FindAsync(gymId);
            if (gym == null) return;

            var reviews = await _context.Reviews
                .Where(r => r.GymId == gymId)
                .ToListAsync();

            if (reviews.Count > 0)
                gym.RatingAverage = (decimal)reviews.Average(r => r.Rating);

            _context.Gyms.Update(gym);
        }

        // =============================================
        // Map entity to DTO
        // =============================================
        private static ReviewResponse MapToResponse(Review review, string fullName)
        {
            return new ReviewResponse
            {
                ReviewId = review.ReviewId,
                UserId = review.UserId,
                UserFullName = fullName,
                GymId = review.GymId,
                ClassId = review.ClassId,
                ClassBookingId = review.ClassBookingId,
                GymBookingId = review.GymBookingId,
                Rating = review.Rating,
                Comment = review.Comment,
                CreatedAt = review.CreatedAt
            };
        }
    }
}
