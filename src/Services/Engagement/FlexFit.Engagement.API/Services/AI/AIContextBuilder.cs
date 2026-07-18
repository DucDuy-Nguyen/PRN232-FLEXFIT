using FlexFit.Engagement.API.Data;
using FlexFit.Engagement.API.Models.DTOs.AI;
using FlexFit.Engagement.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FlexFit.Engagement.API.Services.AI;

/// <summary>
/// Builds AI context from local Engagement data (workout history)
/// and placeholder data for cross-service info (bookings, profiles).
/// In the future, this will call monolith REST API or gRPC for full context.
/// </summary>
public class AIContextBuilder : IAIContextBuilder
{
    private readonly EngagementDbContext _context;

    public AIContextBuilder(EngagementDbContext context)
    {
        _context = context;
    }

    public async Task<AIUserContextDto> GetUserContextAsync(Guid userId)
    {
        // Fetch user from Engagement DB
        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
        if (user == null)
        {
            throw new KeyNotFoundException("Không tìm thấy người dùng này trong hệ thống.");
        }

        var dto = new AIUserContextDto
        {
            UserId = user.UserId,
            UserName = user.Email ?? "",
            FullName = user.FullName ?? "",
            Email = user.Email ?? "",
            Role = "Member" // Default — will be enriched via API aggregation later
        };

        // Fetch local workout history
        var histories = await _context.UserWorkoutHistories
            .Where(h => h.UserId == userId)
            .OrderByDescending(h => h.CreatedAt)
            .Take(10)
            .ToListAsync();

        foreach (var h in histories)
        {
            string type = h.ClassBookingId.HasValue ? "Lớp học" : "Tập tự do phòng Gym";
            dto.WorkoutHistorySummary.Add(
                $"{h.CreatedAt:dd/MM/yyyy} | Thể loại: {type} | Thời lượng: {h.WorkoutDurationMinutes} phút | Calo: {h.CaloriesBurned} kcal");
        }

        // Fetch local reviews summary
        var reviews = await _context.Reviews
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .Take(5)
            .ToListAsync();

        foreach (var r in reviews)
        {
            dto.RecentBookings.Add(
                $"Đánh giá: {r.Rating}/5 sao | Bình luận: {r.Comment ?? "Không có"} | Ngày: {r.CreatedAt:dd/MM/yyyy}");
        }

        // TODO: Call monolith API for bookings, profile, favorites, credits
        // This will be implemented when HTTP clients (IBookingClient, ICatalogClient) are ready.

        return dto;
    }
}
