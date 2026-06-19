using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Flexfit.DTOs;
using Flexfit.DTOs.Review;
using Flexfit.Models;
using Microsoft.EntityFrameworkCore;

namespace Flexfit.Services
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

            // Fetch base data
            var branches = await _db.Branches
                .Include(b => b.Gym)
                .Where(b => b.Gym.OwnerId == ownerId)
                .ToListAsync();

            var branchIds = branches.Select(b => b.BranchId).ToList();

            var classes = await _db.Classes
                .Where(c => branchIds.Contains(c.BranchId))
                .ToListAsync();

            var classIds = classes.Select(c => c.ClassId).ToList();

            var sessions = await _db.GymSessions
                .Where(s => branchIds.Contains(s.BranchId))
                .ToListAsync();

            var sessionIds = sessions.Select(s => s.SessionId).ToList();

            var classBookings = await _db.ClassBookings
                .Where(b => classIds.Contains(b.ClassId) && b.Status == "Confirmed")
                .ToListAsync();

            var gymBookings = await _db.GymBookings
                .Where(b => sessionIds.Contains(b.SessionId) && b.Status == "Confirmed")
                .ToListAsync();

            var currentMonthClassBookings = classBookings.Where(b => b.BookedAt >= startOfMonth).ToList();
            var currentMonthGymBookings = gymBookings.Where(b => b.BookedAt >= startOfMonth).ToList();

            var revenueThisMonth = currentMonthClassBookings.Sum(b => b.CreditUsed) + currentMonthGymBookings.Sum(b => b.CreditUsed);
            var bookingsThisMonth = currentMonthClassBookings.Count + currentMonthGymBookings.Count;

            // Simple unique users logic for new customers
            var allUsersThisMonth = currentMonthClassBookings.Select(b => b.UserId)
                .Union(currentMonthGymBookings.Select(b => b.UserId))
                .Distinct();
                
            var allUsersBeforeThisMonth = classBookings.Where(b => b.BookedAt < startOfMonth).Select(b => b.UserId)
                .Union(gymBookings.Where(b => b.BookedAt < startOfMonth).Select(b => b.UserId))
                .Distinct();

            var newCustomersThisMonth = allUsersThisMonth.Except(allUsersBeforeThisMonth).Count();

            // Occupancy
            var totalCapacity = classes.Sum(c => c.Capacity) + sessions.Sum(s => s.Capacity);
            var totalBooked = classBookings.Count + gymBookings.Count;
            var occupancyRate = totalCapacity > 0 ? (double)totalBooked / totalCapacity * 100 : 0;

            // Charts (6 months)
            var revenueChart = new List<MonthlyMetricDto>();
            var bookingChart = new List<MonthlyMetricDto>();

            for (int i = 5; i >= 0; i--)
            {
                var monthDate = new DateTime(now.Year, now.Month, 1).AddMonths(-i);
                var monthStr = monthDate.ToString("MM/yyyy");
                var endOfThatMonth = monthDate.AddMonths(1);

                var cbs = classBookings.Where(b => b.BookedAt >= monthDate && b.BookedAt < endOfThatMonth).ToList();
                var gbs = gymBookings.Where(b => b.BookedAt >= monthDate && b.BookedAt < endOfThatMonth).ToList();

                revenueChart.Add(new MonthlyMetricDto
                {
                    Month = monthStr,
                    Value = cbs.Sum(b => b.CreditUsed) + gbs.Sum(b => b.CreditUsed)
                });

                bookingChart.Add(new MonthlyMetricDto
                {
                    Month = monthStr,
                    Value = cbs.Count + gbs.Count
                });
            }

            return new PartnerDashboardDto
            {
                RevenueThisMonth = revenueThisMonth,
                BookingsThisMonth = bookingsThisMonth,
                NewCustomersThisMonth = newCustomersThisMonth,
                OccupancyRate = occupancyRate,
                TotalClasses = classIds.Count,
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

            var branchIds = await _db.Branches
                .Where(b => b.Gym.OwnerId == ownerId)
                .Select(b => b.BranchId)
                .ToListAsync();

            var classBookings = await _db.ClassBookings
                .Include(b => b.Class)
                    .ThenInclude(c => c.Branch)
                        .ThenInclude(br => br.Gym)
                .Where(b => b.Class.Branch.Gym.OwnerId == ownerId && b.Status == "Confirmed")
                .ToListAsync();

            var gymBookings = await _db.GymBookings
                .Include(b => b.Session)
                    .ThenInclude(s => s.Branch)
                .Where(b => branchIds.Contains(b.Session.BranchId) && b.Status == "Confirmed")
                .ToListAsync();

            var revenueThisMonth = classBookings.Where(b => b.BookedAt >= startOfMonth).Sum(b => b.CreditUsed)
                                 + gymBookings.Where(b => b.BookedAt >= startOfMonth).Sum(b => b.CreditUsed);
            
            var revenueThisWeek = classBookings.Where(b => b.BookedAt >= startOfWeek).Sum(b => b.CreditUsed)
                                + gymBookings.Where(b => b.BookedAt >= startOfWeek).Sum(b => b.CreditUsed);
                                
            var revenueThisYear = classBookings.Where(b => b.BookedAt >= startOfYear).Sum(b => b.CreditUsed)
                                + gymBookings.Where(b => b.BookedAt >= startOfYear).Sum(b => b.CreditUsed);

            var revenueByBranch = classBookings.Select(b => new { BranchName = b.Class.Branch.BranchName, Credit = b.CreditUsed })
                .Concat(gymBookings.Select(b => new { BranchName = b.Session.Branch.BranchName, Credit = b.CreditUsed }))
                .GroupBy(b => b.BranchName)
                .Select(g => new { Name = g.Key, Total = g.Sum(x => x.Credit) })
                .OrderByDescending(g => g.Total)
                .ToList();

            var revenueByClass = classBookings
                .Where(b => b.Class != null)
                .GroupBy(b => b.Class.ClassName)
                .Select(g => new { Name = g.Key ?? "Lớp học", Total = g.Sum(x => x.CreditUsed) })
                .OrderByDescending(g => g.Total)
                .ToList();
                
            var topClass = revenueByClass.FirstOrDefault()?.Name;
            var topBranch = revenueByBranch.FirstOrDefault()?.Name;

            var revenueByMonth = new List<NameTotalDto>();
            for (int i = 5; i >= 0; i--)
            {
                var monthDate = new DateTime(now.Year, now.Month, 1).AddMonths(-i);
                var monthStr = monthDate.ToString("MM/yyyy");
                var endOfThatMonth = monthDate.AddMonths(1);

                var cbs = classBookings.Where(b => b.BookedAt >= monthDate && b.BookedAt < endOfThatMonth).ToList();
                var gbs = gymBookings.Where(b => b.BookedAt >= monthDate && b.BookedAt < endOfThatMonth).ToList();

                revenueByMonth.Add(new NameTotalDto
                {
                    Name = monthStr,
                    Total = cbs.Sum(b => b.CreditUsed) + gbs.Sum(b => b.CreditUsed)
                });
            }

            return new PartnerRevenueDto
            {
                TotalRevenue = revenueThisYear,
                RevenueThisMonth = revenueThisMonth,
                RevenueThisWeek = revenueThisWeek,
                RevenueThisYear = revenueThisYear,
                TopClassName = topClass,
                TopBranchName = topBranch,
                RevenueByBranch = revenueByBranch.Select(x => new NameTotalDto { Name = x.Name, Total = x.Total }).ToList(),
                RevenueByClass = revenueByClass.Select(x => new NameTotalDto { Name = x.Name, Total = x.Total }).ToList(),
                RevenueByMonth = revenueByMonth
            };
        }
    }
}
