using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Flexfit.DTOs;
using Flexfit.DTOs.Review;
using Flexfit.Models;
using Microsoft.EntityFrameworkCore;

namespace Flexfit.Service
{
    public class PartnerService : IPartnerService
    {
        private readonly FlexFitDbContext _db;

        public PartnerService(FlexFitDbContext db)
        {
            _db = db;
        }

        public async Task<PartnerDashboardDto> GetDashboardStatsAsync(Guid ownerId)
        {
            var now = DateTime.Now;
            var startOfMonth = new DateTime(now.Year, now.Month, 1);
            var lastMonthStart = startOfMonth.AddMonths(-1);

            // 1. Lấy danh sách ID chi nhánh thuộc sở hữu của Partner
            var branchIds = await _db.Branches
                .Where(b => b.Gym.OwnerId == ownerId)
                .Select(b => b.BranchId)
                .ToListAsync();

            if (branchIds.Count == 0)
            {
                return new PartnerDashboardDto
                {
                    RevenueThisMonth = 0,
                    BookingsThisMonth = 0,
                    NewCustomersThisMonth = 0,
                    OccupancyRate = 0,
                    TotalClasses = 0,
                    TotalBranches = 0,
                    RevenueChart = new List<MonthlyMetricDto>(),
                    BookingChart = new List<MonthlyMetricDto>()
                };
            }

            // 2. Lấy thông tin lớp học và session gym để tính Capacity
            var classesQuery = _db.Classes.Where(c => branchIds.Contains(c.BranchId));
            var sessionsQuery = _db.GymSessions.Where(s => branchIds.Contains(s.BranchId));

            var totalClasses = await classesQuery.CountAsync();
            var totalCapacity = await classesQuery.SumAsync(c => c.Capacity) + await sessionsQuery.SumAsync(s => s.Capacity);

            var classIds = await classesQuery.Select(c => c.ClassId).ToListAsync();
            var sessionIds = await sessionsQuery.Select(s => s.SessionId).ToListAsync();

            // 3. IQueryable Bookings (chưa kéo về RAM)
            var classBookingsQuery = _db.ClassBookings
                .Where(b => classIds.Contains(b.ClassId) && b.Status == "Confirmed");

            var gymBookingsQuery = _db.GymBookings
                .Where(b => sessionIds.Contains(b.SessionId) && b.Status == "Confirmed");

            // 4. Doanh thu và lượng booking tháng này tính trực tiếp trên Database
            var currentMonthClassRevenue = await classBookingsQuery.Where(b => b.BookedAt >= startOfMonth).SumAsync(b => b.CreditUsed);
            var currentMonthGymRevenue = await gymBookingsQuery.Where(b => b.BookedAt >= startOfMonth).SumAsync(b => b.CreditUsed);

            var currentMonthClassBookingsCount = await classBookingsQuery.Where(b => b.BookedAt >= startOfMonth).CountAsync();
            var currentMonthGymBookingsCount = await gymBookingsQuery.Where(b => b.BookedAt >= startOfMonth).CountAsync();

            var revenueThisMonth = currentMonthClassRevenue + currentMonthGymRevenue;
            var bookingsThisMonth = currentMonthClassBookingsCount + currentMonthGymBookingsCount;

            // 5. Tính số lượng khách hàng mới trực tiếp trên Database
            var allUsersThisMonthQuery = classBookingsQuery.Where(b => b.BookedAt >= startOfMonth).Select(b => b.UserId)
                .Union(gymBookingsQuery.Where(b => b.BookedAt >= startOfMonth).Select(b => b.UserId));

            var allUsersBeforeThisMonthQuery = classBookingsQuery.Where(b => b.BookedAt < startOfMonth).Select(b => b.UserId)
                .Union(gymBookingsQuery.Where(b => b.BookedAt < startOfMonth).Select(b => b.UserId));

            // Dùng Except để tìm những UserId có trong tháng này nhưng chưa từng có ở các tháng trước
            var newCustomersThisMonth = await allUsersThisMonthQuery.Except(allUsersBeforeThisMonthQuery).CountAsync();

            // 6. Tính tỷ lệ lấp đầy (Occupancy)
            var totalBooked = await classBookingsQuery.CountAsync() + await gymBookingsQuery.CountAsync();
            var occupancyRate = totalCapacity > 0 ? (double)totalBooked / totalCapacity * 100 : 0;

            // 7. Nhóm dữ liệu biểu đồ 6 tháng trực tiếp trên Database bằng GroupBy
            var sixMonthsAgo = startOfMonth.AddMonths(-5);

            var monthlyClassStats = await classBookingsQuery
                .Where(b => b.BookedAt >= sixMonthsAgo)
                .GroupBy(b => new { b.BookedAt.Year, b.BookedAt.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Revenue = g.Sum(x => x.CreditUsed), Bookings = g.Count() })
                .ToListAsync();

            var monthlyGymStats = await gymBookingsQuery
                .Where(b => b.BookedAt >= sixMonthsAgo)
                .GroupBy(b => new { b.BookedAt.Year, b.BookedAt.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Revenue = g.Sum(x => x.CreditUsed), Bookings = g.Count() })
                .ToListAsync();

            var revenueChart = new List<MonthlyMetricDto>();
            var bookingChart = new List<MonthlyMetricDto>();

            for (int i = 5; i >= 0; i--)
            {
                var monthDate = startOfMonth.AddMonths(-i);
                var monthStr = monthDate.ToString("MM/yyyy");

                var classStat = monthlyClassStats.FirstOrDefault(s => s.Year == monthDate.Year && s.Month == monthDate.Month);
                var gymStat = monthlyGymStats.FirstOrDefault(s => s.Year == monthDate.Year && s.Month == monthDate.Month);

                var totalRevenueVal = (classStat?.Revenue ?? 0) + (gymStat?.Revenue ?? 0);
                var totalBookingsVal = (classStat?.Bookings ?? 0) + (gymStat?.Bookings ?? 0);

                revenueChart.Add(new MonthlyMetricDto
                {
                    Month = monthStr,
                    Value = totalRevenueVal
                });

                bookingChart.Add(new MonthlyMetricDto
                {
                    Month = monthStr,
                    Value = totalBookingsVal
                });
            }

            return new PartnerDashboardDto
            {
                RevenueThisMonth = revenueThisMonth,
                BookingsThisMonth = bookingsThisMonth,
                NewCustomersThisMonth = newCustomersThisMonth,
                OccupancyRate = occupancyRate,
                TotalClasses = totalClasses,
                TotalBranches = branchIds.Count,
                RevenueChart = revenueChart,
                BookingChart = bookingChart
            };
        }

        public async Task<IEnumerable<PartnerCustomerDto>> GetCustomersAsync(Guid ownerId)
        {
            var branchIds = await _db.Branches
                .Where(b => b.Gym.OwnerId == ownerId)
                .Select(b => b.BranchId)
                .ToListAsync();

            var classBookings = await _db.ClassBookings
                .Include(b => b.User)
                .Include(b => b.Class)
                .Where(b => branchIds.Contains(b.Class.BranchId) && b.Status == "Confirmed")
                .ToListAsync();

            var gymBookings = await _db.GymBookings
                .Include(b => b.User)
                .Include(b => b.Session)
                .Where(b => branchIds.Contains(b.Session.BranchId) && b.Status == "Confirmed")
                .ToListAsync();

            var allBookings = classBookings.Select(b => new { b.UserId, b.User, b.CreditUsed, b.BookedAt })
                .Concat(gymBookings.Select(b => new { b.UserId, b.User, b.CreditUsed, b.BookedAt }))
                .ToList();

            var customers = allBookings
                .GroupBy(b => b.UserId)
                .Select(g => new PartnerCustomerDto
                {
                    UserId = g.Key,
                    FullName = g.First().User.FullName,
                    Email = g.First().User.Email,
                    PhoneNumber = g.First().User.PhoneNumber,
                    TotalBookings = g.Count(),
                    TotalCreditUsed = g.Sum(x => x.CreditUsed),
                    LastBookingAt = g.Max(x => x.BookedAt)
                })
                .OrderByDescending(c => c.LastBookingAt)
                .ToList();

            return customers;
        }

        public async Task<IEnumerable<ReviewResponse>> GetReviewsAsync(Guid ownerId)
        {
            var gymIds = await _db.Gyms
                .Where(g => g.OwnerId == ownerId)
                .Select(g => g.GymId)
                .ToListAsync();

            var branchIds = await _db.Branches
                .Where(b => gymIds.Contains(b.GymId))
                .Select(b => b.BranchId)
                .ToListAsync();

            var classIds = await _db.Classes
                .Where(c => branchIds.Contains(c.BranchId))
                .Select(c => c.ClassId)
                .ToListAsync();

            if (gymIds.Count == 0 && classIds.Count == 0)
            {
                return Enumerable.Empty<ReviewResponse>();
            }

            var reviews = await _db.Reviews
                .AsNoTracking()
                .Include(r => r.User)
                .Include(r => r.Gym)
                .Include(r => r.Class)
                    .ThenInclude(c => c.Branch)
                        .ThenInclude(b => b.Gym)
                .Where(r => 
                    (r.GymId.HasValue && gymIds.Contains(r.GymId.Value)) ||
                    (r.ClassId.HasValue && classIds.Contains(r.ClassId.Value)))
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return reviews.Select(r => new ReviewResponse
            {
                ReviewId = r.ReviewId,
                UserId = r.UserId,
                UserFullName = r.User != null ? r.User.FullName : "Khách hàng",
                GymId = r.GymId ?? r.Class?.Branch?.GymId,
                GymName = r.Gym != null ? r.Gym.GymName : r.Class?.Branch?.Gym?.GymName,
                ClassId = r.ClassId,
                ClassName = r.Class != null ? r.Class.ClassName : null,
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt
            });
        }

        public async Task<PartnerRevenueDto> GetRevenueAsync(Guid ownerId)
        {
            var now = DateTime.Now;
            var startOfMonth = new DateTime(now.Year, now.Month, 1);
            var startOfWeek = now.AddDays(-(int)now.DayOfWeek + (int)DayOfWeek.Monday);
            var startOfYear = new DateTime(now.Year, 1, 1);
            var sixMonthsAgo = startOfMonth.AddMonths(-5);

            var branchIds = await _db.Branches
                .Where(b => b.Gym.OwnerId == ownerId)
                .Select(b => b.BranchId)
                .ToListAsync();

            if (branchIds.Count == 0)
            {
                return new PartnerRevenueDto
                {
                    TotalRevenue = 0, RevenueThisMonth = 0, RevenueThisWeek = 0, RevenueThisYear = 0,
                    RevenueByBranch = new List<NameTotalDto>(),
                    RevenueByClass = new List<NameTotalDto>(),
                    RevenueByMonth = new List<NameTotalDto>()
                };
            }

            // IQueryable — chưa gọi DB
            var classBookingsQuery = _db.ClassBookings
                .Where(b => branchIds.Contains(b.Class.BranchId) && b.Status == "Confirmed");

            var gymBookingsQuery = _db.GymBookings
                .Where(b => branchIds.Contains(b.Session.BranchId) && b.Status == "Confirmed");

            // Các con số tổng: await tuần tự (EF Core DbContext KHÔNG thread-safe)
            var revenueThisMonth = await classBookingsQuery.Where(b => b.BookedAt >= startOfMonth).SumAsync(b => b.CreditUsed)
                                 + await gymBookingsQuery.Where(b => b.BookedAt >= startOfMonth).SumAsync(b => b.CreditUsed);

            var revenueThisWeek = await classBookingsQuery.Where(b => b.BookedAt >= startOfWeek).SumAsync(b => b.CreditUsed)
                                + await gymBookingsQuery.Where(b => b.BookedAt >= startOfWeek).SumAsync(b => b.CreditUsed);

            var revenueThisYear = await classBookingsQuery.Where(b => b.BookedAt >= startOfYear).SumAsync(b => b.CreditUsed)
                                + await gymBookingsQuery.Where(b => b.BookedAt >= startOfYear).SumAsync(b => b.CreditUsed);

            // GroupBy chi nhánh — thực thi trên DB
            var classByBranch = await classBookingsQuery
                .GroupBy(b => b.Class.Branch.BranchName)
                .Select(g => new NameTotalDto { Name = g.Key, Total = g.Sum(x => x.CreditUsed) })
                .ToListAsync();

            var gymByBranch = await gymBookingsQuery
                .GroupBy(b => b.Session.Branch.BranchName)
                .Select(g => new NameTotalDto { Name = g.Key, Total = g.Sum(x => x.CreditUsed) })
                .ToListAsync();

            var revenueByBranch = classByBranch
                .Concat(gymByBranch)
                .GroupBy(x => x.Name)
                .Select(g => new NameTotalDto { Name = g.Key, Total = g.Sum(x => x.Total) })
                .OrderByDescending(x => x.Total)
                .ToList();

            // GroupBy lớp học — thực thi trên DB
            var revenueByClass = await classBookingsQuery
                .GroupBy(b => b.Class.ClassName)
                .Select(g => new NameTotalDto { Name = g.Key ?? "Lớp học", Total = g.Sum(x => x.CreditUsed) })
                .OrderByDescending(g => g.Total)
                .ToListAsync();

            var topClass  = revenueByClass.FirstOrDefault()?.Name;
            var topBranch = revenueByBranch.FirstOrDefault()?.Name;

            // Biểu đồ 6 tháng — GroupBy trực tiếp trên DB
            var rawClassMonthly = await classBookingsQuery
                .Where(b => b.BookedAt >= sixMonthsAgo)
                .GroupBy(b => new { b.BookedAt.Year, b.BookedAt.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Total = g.Sum(x => x.CreditUsed) })
                .ToListAsync();

            var rawGymMonthly = await gymBookingsQuery
                .Where(b => b.BookedAt >= sixMonthsAgo)
                .GroupBy(b => new { b.BookedAt.Year, b.BookedAt.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Total = g.Sum(x => x.CreditUsed) })
                .ToListAsync();

            var revenueByMonth = Enumerable.Range(0, 6).Select(i =>
            {
                var monthDate = startOfMonth.AddMonths(-5 + i);
                var monthStr = monthDate.ToString("MM/yyyy");
                var classTotal = rawClassMonthly.FirstOrDefault(x => x.Year == monthDate.Year && x.Month == monthDate.Month)?.Total ?? 0;
                var gymTotal   = rawGymMonthly.FirstOrDefault(x => x.Year == monthDate.Year && x.Month == monthDate.Month)?.Total ?? 0;
                return new NameTotalDto { Name = monthStr, Total = classTotal + gymTotal };
            }).ToList();

            return new PartnerRevenueDto
            {
                TotalRevenue = revenueThisYear,
                RevenueThisMonth = revenueThisMonth,
                RevenueThisWeek = revenueThisWeek,
                RevenueThisYear = revenueThisYear,
                TopClassName = topClass,
                TopBranchName = topBranch,
                RevenueByBranch = revenueByBranch,
                RevenueByClass = revenueByClass,
                RevenueByMonth = revenueByMonth
            };
        }
    }
}

