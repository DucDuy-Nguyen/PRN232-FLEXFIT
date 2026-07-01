using Flexfit.DTOs.AdminRevenue;
using Flexfit.Helpers;
using Flexfit.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Flexfit.Controllers;

[ApiController]
[Route("api/admin/revenue")]
[Authorize(Roles = "Admin")]
public class AdminRevenueController : ControllerBase
{
    private static readonly string[] SuccessfulStatuses = ["Paid", "Success", "Completed"];
    private readonly FlexFitDbContext _context;

    public AdminRevenueController(FlexFitDbContext context)
    {
        _context = context;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<AdminRevenueSummaryResponse>> GetSummary()
    {
        var now = DateTimeHelper.GetVietnamTime();
        var todayStart = now.Date;
        var tomorrowStart = todayStart.AddDays(1);
        var monthStart = new DateTime(now.Year, now.Month, 1);
        var nextMonthStart = monthStart.AddMonths(1);
        var chartStart = monthStart.AddMonths(-5);

        // 1. Định nghĩa Query IQueryable các payments thành công
        var successfulPaymentsQuery = _context.Payments
            .AsNoTracking()
            .Where(payment => SuccessfulStatuses.Contains(payment.Status));

        // 2. Chạy tính toán tổng số tiền và số lượng direct trên Database (chạy song song qua DB)
        var totalRevenueThisMonth = await successfulPaymentsQuery
            .Where(payment => (payment.PaidAt ?? payment.CreatedAt) >= monthStart && (payment.PaidAt ?? payment.CreatedAt) < nextMonthStart)
            .SumAsync(payment => payment.Amount);

        var successfulPaymentCount = await successfulPaymentsQuery.CountAsync();

        var totalCreditsPaid = await successfulPaymentsQuery
            .Where(payment => (payment.PaidAt ?? payment.CreatedAt) >= monthStart && (payment.PaidAt ?? payment.CreatedAt) < nextMonthStart)
            .SumAsync(payment => payment.Package.CreditAmount + payment.Package.BonusCredit);

        var revenueToday = await successfulPaymentsQuery
            .Where(payment => (payment.PaidAt ?? payment.CreatedAt) >= todayStart && (payment.PaidAt ?? payment.CreatedAt) < tomorrowStart)
            .SumAsync(payment => payment.Amount);

        // 3. Nhóm doanh thu 6 tháng (MonthlyRevenue) bằng GroupBy ở Database
        var rawMonthlyRevenue = await successfulPaymentsQuery
            .Where(payment => (payment.PaidAt ?? payment.CreatedAt) >= chartStart)
            .GroupBy(payment => new { Year = (payment.PaidAt ?? payment.CreatedAt).Year, Month = (payment.PaidAt ?? payment.CreatedAt).Month })
            .Select(group => new
            {
                group.Key.Year,
                group.Key.Month,
                Revenue = group.Sum(payment => payment.Amount)
            })
            .ToListAsync();

        var monthlyRevenue = Enumerable.Range(0, 6)
            .Select(index =>
            {
                var monthDate = chartStart.AddMonths(index);
                var match = rawMonthlyRevenue.FirstOrDefault(r => r.Year == monthDate.Year && r.Month == monthDate.Month);

                return new MonthlyRevenueItem
                {
                    Month = monthDate.ToString("yyyy-MM"),
                    Revenue = match?.Revenue ?? 0,
                };
            })
            .ToList();

        // 4. Nhóm doanh thu theo gói tập tháng này bằng GroupBy ở Database
        var packageSales = await successfulPaymentsQuery
            .Where(payment => (payment.PaidAt ?? payment.CreatedAt) >= monthStart && (payment.PaidAt ?? payment.CreatedAt) < nextMonthStart)
            .GroupBy(payment => payment.Package.PackageName)
            .Select(group => new PackageSalesItem
            {
                PackageName = group.Key,
                Count = group.Count(),
                Revenue = group.Sum(payment => payment.Amount),
            })
            .OrderByDescending(item => item.Revenue)
            .ThenBy(item => item.PackageName)
            .ToListAsync();

        return Ok(new AdminRevenueSummaryResponse
        {
            TotalRevenueThisMonth = totalRevenueThisMonth,
            SuccessfulPaymentCount = successfulPaymentCount,
            TotalCreditsPaid = totalCreditsPaid,
            RevenueToday = revenueToday,
            MonthlyRevenue = monthlyRevenue,
            PackageSales = packageSales,
        });
    }
}
