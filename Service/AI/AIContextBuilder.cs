using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Flexfit.DTOs.AI;
using Flexfit.Models;
using Microsoft.EntityFrameworkCore;

namespace Flexfit.Service.AI;

public class AIContextBuilder : IAIContextBuilder
{
    private readonly FlexFitDbContext _context;

    public AIContextBuilder(FlexFitDbContext context)
    {
        _context = context;
    }

    public async Task<AIUserContextDto> BuildUserContextAsync(Guid userId)
    {
        // 1. Fetch User with Roles
        var user = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (user == null)
        {
            throw new KeyNotFoundException("Không tìm thấy người dùng này trong hệ thống.");
        }

        var roles = user.UserRoles.Select(ur => ur.Role?.RoleName ?? "Member").ToList();
        if (!roles.Any())
        {
            roles.Add("Member");
        }

        var dto = new AIUserContextDto
        {
            UserId = user.UserId,
            UserName = user.Email,
            FullName = user.FullName,
            Email = user.Email,
            Role = string.Join(", ", roles)
        };

        // 2. Fetch specific context information depending on the roles the user has
        if (roles.Any(r => r.Equals("Member", StringComparison.OrdinalIgnoreCase)))
        {
            await PopulateMemberContextAsync(dto);
        }
        
        if (roles.Any(r => r.Equals("Partner", StringComparison.OrdinalIgnoreCase) || r.Equals("GymPartner", StringComparison.OrdinalIgnoreCase)))
        {
            await PopulatePartnerContextAsync(dto);
        }
        
        if (roles.Any(r => r.Equals("Staff", StringComparison.OrdinalIgnoreCase)))
        {
            await PopulateStaffContextAsync(dto);
        }
        
        if (roles.Any(r => r.Equals("Admin", StringComparison.OrdinalIgnoreCase)))
        {
            await PopulateAdminContextAsync(dto);
        }

        return dto;
    }

    private async Task PopulateMemberContextAsync(AIUserContextDto dto)
    {
        // Fetch Credits
        var userCredit = await _context.UserCredits.FirstOrDefaultAsync(c => c.UserId == dto.UserId);
        dto.Credits = userCredit?.Balance ?? 0;

        // Fetch Member Profile
        var profile = await _context.MemberProfiles.FirstOrDefaultAsync(p => p.UserId == dto.UserId);
        if (profile != null)
        {
            dto.FitnessGoal = profile.FitnessGoal ?? string.Empty;
            dto.Height = profile.HeightCm;
            dto.Weight = profile.WeightKg;
        }

        // Fetch Favorite Gyms
        dto.FavoriteGyms = await _context.FavoriteGyms
            .Where(f => f.UserId == dto.UserId)
            .Include(f => f.Gym)
            .Select(f => f.Gym.GymName)
            .ToListAsync();

        // Fetch Favorite Classes
        dto.FavoriteClasses = await _context.FavoriteClasses
            .Where(f => f.UserId == dto.UserId)
            .Include(f => f.Class)
            .Select(f => f.Class.ClassName)
            .ToListAsync();

        // Fetch Recent Class Bookings
        var classBookings = await _context.ClassBookings
            .Where(b => b.UserId == dto.UserId)
            .OrderByDescending(b => b.BookedAt)
            .Take(5)
            .Include(b => b.Class)
            .ToListAsync();

        foreach (var cb in classBookings)
        {
            dto.RecentBookings.Add($"Lớp: {cb.Class.ClassName} | Ngày đặt: {cb.BookedAt:dd/MM/yyyy} | Giá: {cb.CreditUsed} credits | Trạng thái: {cb.Status} (Checkin: {cb.CheckInStatus})");
        }

        // Fetch Recent Gym Bookings
        var gymBookings = await _context.GymBookings
            .Where(b => b.UserId == dto.UserId)
            .OrderByDescending(b => b.BookedAt)
            .Take(5)
            .Include(b => b.Session)
                .ThenInclude(s => s.Branch)
            .ToListAsync();

        foreach (var gb in gymBookings)
        {
            dto.RecentBookings.Add($"Tập tự do tại: {gb.Session.Branch?.BranchName} | Ngày đặt: {gb.BookedAt:dd/MM/yyyy} | Giá: {gb.CreditUsed} credits | Trạng thái: {gb.Status} (Checkin: {gb.CheckInStatus})");
        }

        // Fetch Workout History
        var histories = await _context.UserWorkoutHistories
            .Where(h => h.UserId == dto.UserId)
            .OrderByDescending(h => h.CreatedAt)
            .Take(5)
            .Include(h => h.ClassBooking)
                .ThenInclude(cb => cb!.Class)
            .ToListAsync();

        foreach (var h in histories)
        {
            string type = h.ClassBookingId.HasValue ? $"Lớp học {h.ClassBooking?.Class?.ClassName}" : "Tập tự do phòng Gym";
            dto.WorkoutHistorySummary.Add($"{h.CreatedAt:dd/MM/yyyy} | Thể loại: {type} | Thời lượng: {h.WorkoutDurationMinutes} phút | Calo: {h.CaloriesBurned} kcal");
        }
    }

    private async Task PopulatePartnerContextAsync(AIUserContextDto dto)
    {
        // Query Partner Gyms owned by this Partner
        var gyms = await _context.Gyms
            .Where(g => g.OwnerId == dto.UserId)
            .Include(g => g.Branches)
            .ToListAsync();

        foreach (var gym in gyms)
        {
            var branchList = gym.Branches.Select(b => b.BranchName).ToList();
            string branchesStr = branchList.Any() ? string.Join(", ", branchList) : "Chưa có chi nhánh";
            dto.OwnedGyms.Add($"Tên phòng Gym: {gym.GymName} | Trạng thái: {gym.Status} | Các chi nhánh: [{branchesStr}] | Số đánh giá: {gym.TotalReviews} (Trung bình {gym.RatingAverage}/5)");
        }

        // Aggregate Partner Revenue summary if applicable
        var totalPayments = await _context.Payments
            .Where(p => p.Status == "Paid" && gyms.Select(g => g.GymId).Contains(p.UserId)) // Simplified logic
            .SumAsync(p => p.Amount);

        dto.PartnerSummary = $"Bạn hiện sở hữu {gyms.Count} hệ thống phòng Gym đang hoạt động trong hệ thống FlexFit.";
    }

    private async Task PopulateStaffContextAsync(AIUserContextDto dto)
    {
        // Query Managed Branches
        var staffBranches = await _context.BranchStaffs
            .Where(s => s.StaffId == dto.UserId)
            .Include(s => s.Branch)
                .ThenInclude(b => b.Classes)
            .ToListAsync();

        foreach (var sb in staffBranches)
        {
            dto.ManagedBranches.Add($"Chi nhánh quản lý: {sb.Branch.BranchName} | Giao lúc: {sb.AssignedAt:dd/MM/yyyy}");
            
            var today = DateTime.UtcNow.Date;
            var classesToday = sb.Branch.Classes
                .Where(c => c.StartTime.Date == today)
                .Select(c => $"{c.ClassName} ({c.StartTime:HH:mm} - {c.EndTime:HH:mm})")
                .ToList();

            if (classesToday.Any())
            {
                dto.StaffSummary += $"\nChi nhánh {sb.Branch.BranchName} hôm nay có các lớp: {string.Join(", ", classesToday)}";
            }
        }
    }

    private async Task PopulateAdminContextAsync(AIUserContextDto dto)
    {
        var totalUsers = await _context.Users.CountAsync();
        var totalGyms = await _context.Gyms.CountAsync();
        var totalClasses = await _context.Classes.CountAsync();
        var totalBookings = await _context.ClassBookings.CountAsync() + await _context.GymBookings.CountAsync();

        dto.AdminSummary = $"Hệ thống FlexFit hiện đang có: {totalUsers} người dùng đăng ký, {totalGyms} chuỗi phòng tập đối tác, {totalClasses} lớp học và tổng số {totalBookings} lượt booking trên toàn hệ thống.";
    }
}
