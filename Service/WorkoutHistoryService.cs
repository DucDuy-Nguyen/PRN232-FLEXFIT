using Flexfit.DTOs.WorkoutHistory;
using Flexfit.Helpers;
using Flexfit.Models;
using Flexfit.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Flexfit.Service
{
    public class WorkoutHistoryService : IWorkoutHistoryService
    {
        private readonly IWorkoutHistoryRepository _historyRepo;
        private readonly FlexFitDbContext _context;

        public WorkoutHistoryService(IWorkoutHistoryRepository historyRepo, FlexFitDbContext context)
        {
            _historyRepo = historyRepo;
            _context = context;
        }

        public async Task CreateHistoryFromCheckInAsync(Guid userId, Guid? classBookingId, Guid? gymBookingId)
        {
            // Tránh tạo trùng lặp
            if (classBookingId.HasValue)
            {
                var exists = await _context.UserWorkoutHistories.AnyAsync(h => h.ClassBookingId == classBookingId.Value);
                if (exists) return;
            }
            if (gymBookingId.HasValue)
            {
                var exists = await _context.UserWorkoutHistories.AnyAsync(h => h.GymBookingId == gymBookingId.Value);
                if (exists) return;
            }

            int calories = 0;
            int duration = 0;

            if (classBookingId.HasValue)
            {
                var booking = await _context.ClassBookings
                    .Include(b => b.Class)
                    .FirstOrDefaultAsync(b => b.BookingId == classBookingId.Value);

                if (booking != null && booking.Class != null)
                {
                    duration = (int)(booking.Class.EndTime - booking.Class.StartTime).TotalMinutes;
                    if (duration <= 0) duration = 60; // Mặc định 1 tiếng nếu bị lỗi cấu hình thời gian

                    calories = booking.Class.CaloriesBurnEstimate ?? (duration * 6); // Ước tính 6 calo / phút cho lớp học
                }
            }
            else if (gymBookingId.HasValue)
            {
                var booking = await _context.GymBookings
                    .Include(b => b.Session)
                    .FirstOrDefaultAsync(b => b.BookingId == gymBookingId.Value);

                if (booking != null && booking.Session != null)
                {
                    duration = (int)(booking.Session.EndTime - booking.Session.StartTime).TotalMinutes;
                    if (duration <= 0) duration = 60; // Mặc định 1 tiếng nếu bị lỗi cấu hình thời gian

                    calories = duration * 5; // Ước tính 5 calo / phút cho tập tự do
                }
            }

            var history = new UserWorkoutHistory
            {
                WorkoutHistoryId = Guid.NewGuid(),
                UserId = userId,
                ClassBookingId = classBookingId,
                GymBookingId = gymBookingId,
                CaloriesBurned = calories,
                WorkoutDurationMinutes = duration,
                CreatedAt = DateTimeHelper.GetVietnamTime()
            };

            await _historyRepo.AddAsync(history);
            await _historyRepo.SaveChangesAsync();
        }

        public async Task<IEnumerable<WorkoutHistoryDto>> GetMyWorkoutHistoryAsync(Guid userId, DateTime? startDate, DateTime? endDate)
        {
            var histories = await _historyRepo.GetByUserIdAsync(userId, startDate, endDate);
            return histories.Select(MapToDto);
        }

        public async Task<WorkoutStatisticsResponse> GetWorkoutStatisticsAsync(Guid userId)
        {
            var histories = (await _historyRepo.GetByUserIdAsync(userId, null, null)).ToList();

            var totalWorkouts = histories.Count;
            var totalGym = histories.Count(h => h.GymBookingId.HasValue);
            var totalClass = histories.Count(h => h.ClassBookingId.HasValue);
            var totalCalories = histories.Sum(h => h.CaloriesBurned ?? 0);
            var totalDuration = histories.Sum(h => h.WorkoutDurationMinutes ?? 0);
            var averageCalories = totalWorkouts > 0 ? Math.Round((double)totalCalories / totalWorkouts, 1) : 0;

            // Tính thống kê tuần hiện tại (Từ Thứ Hai đến Chủ Nhật)
            var now = DateTimeHelper.GetVietnamTime();
            int diff = (7 + (now.DayOfWeek - DayOfWeek.Monday)) % 7;
            var startOfWeek = now.AddDays(-1 * diff).Date; // Đầu tuần (Thứ Hai 00:00:00)
            var endOfWeek = startOfWeek.AddDays(7); // Cuối tuần (Chủ Nhật 23:59:59)

            var weeklyHistories = histories
                .Where(h => h.CreatedAt >= startOfWeek && h.CreatedAt < endOfWeek)
                .ToList();

            var weeklyStats = new List<DailyWorkoutStatDto>();
            string[] dayNames = { "Thứ Hai", "Thứ Ba", "Thứ Tư", "Thứ Năm", "Thứ Sáu", "Thứ Bảy", "Chủ Nhật" };
            
            for (int i = 0; i < 7; i++)
            {
                var dayDate = startOfWeek.AddDays(i);
                var dayHistories = weeklyHistories.Where(h => h.CreatedAt.Date == dayDate).ToList();

                weeklyStats.Add(new DailyWorkoutStatDto
                {
                    DayOfWeek = dayNames[i],
                    WorkoutCount = dayHistories.Count,
                    CaloriesBurned = dayHistories.Sum(h => h.CaloriesBurned ?? 0)
                });
            }

            return new WorkoutStatisticsResponse
            {
                TotalWorkouts = totalWorkouts,
                TotalGymSessions = totalGym,
                TotalClassSessions = totalClass,
                TotalCaloriesBurned = totalCalories,
                TotalDurationMinutes = totalDuration,
                AverageCaloriesPerSession = averageCalories,
                WeeklyStats = weeklyStats
            };
        }

        public async Task<WorkoutHistoryDto> UpdateWorkoutStatsAsync(Guid userId, Guid historyId, UpdateWorkoutHistoryRequest request)
        {
            var history = await _historyRepo.GetByIdAsync(historyId);
            if (history == null || history.UserId != userId)
            {
                throw new KeyNotFoundException("Không tìm thấy lịch sử tập luyện này hoặc lịch sử này không thuộc về bạn.");
            }

            history.CaloriesBurned = request.CaloriesBurned;
            history.WorkoutDurationMinutes = request.WorkoutDurationMinutes;

            await _historyRepo.UpdateAsync(history);
            await _historyRepo.SaveChangesAsync();

            return MapToDto(history);
        }

        private static WorkoutHistoryDto MapToDto(UserWorkoutHistory history)
        {
            string workoutType = "Gym";
            string name = "Tập tự do";
            string? branchName = null;
            string? gymName = null;
            Guid? bookingId = null;

            if (history.ClassBookingId.HasValue && history.ClassBooking != null)
            {
                workoutType = "Class";
                bookingId = history.ClassBookingId;
                name = history.ClassBooking.Class?.ClassName ?? "Lớp học";
                branchName = history.ClassBooking.Class?.Branch?.BranchName;
                gymName = history.ClassBooking.Class?.Branch?.Gym?.GymName;
            }
            else if (history.GymBookingId.HasValue && history.GymBooking != null)
            {
                workoutType = "Gym";
                bookingId = history.GymBookingId;
                name = "Tập tự do";
                branchName = history.GymBooking.Session?.Branch?.BranchName;
                gymName = history.GymBooking.Session?.Branch?.Gym?.GymName;
            }

            return new WorkoutHistoryDto
            {
                WorkoutHistoryId = history.WorkoutHistoryId,
                BookingId = bookingId,
                WorkoutType = workoutType,
                Name = name,
                BranchName = branchName,
                GymName = gymName,
                CaloriesBurned = history.CaloriesBurned ?? 0,
                WorkoutDurationMinutes = history.WorkoutDurationMinutes ?? 0,
                WorkoutDate = history.CreatedAt
            };
        }
    }
}
