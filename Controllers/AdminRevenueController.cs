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

        var successfulPayments = _context.Payments
            .AsNoTracking()
            .Include(payment => payment.Package)
            .Where(payment => SuccessfulStatuses.Contains(payment.Status));

        var payments = await successfulPayments
            .Select(payment => new
            {
                payment.Amount,
                PaidDate = payment.PaidAt ?? payment.CreatedAt,
                payment.Package.PackageName,
                payment.Package.CreditAmount,
                payment.Package.BonusCredit,
            })
            .ToListAsync();

        var thisMonthPayments = payments
            .Where(payment => payment.PaidDate >= monthStart && payment.PaidDate < nextMonthStart)
            .ToList();

        var monthlyRevenue = Enumerable.Range(0, 6)
            .Select(index =>
            {
                var month = chartStart.AddMonths(index);
                var nextMonth = month.AddMonths(1);

                return new MonthlyRevenueItem
                {
                    Month = month.ToString("yyyy-MM"),
                    Revenue = payments
                        .Where(payment => payment.PaidDate >= month && payment.PaidDate < nextMonth)
                        .Sum(payment => payment.Amount),
                };
            })
            .ToList();

        var packageSales = thisMonthPayments
            .GroupBy(payment => payment.PackageName)
            .Select(group => new PackageSalesItem
            {
                PackageName = group.Key,
                Count = group.Count(),
                Revenue = group.Sum(payment => payment.Amount),
            })
            .OrderByDescending(item => item.Revenue)
            .ThenBy(item => item.PackageName)
            .ToList();

        return Ok(new AdminRevenueSummaryResponse
        {
            TotalRevenueThisMonth = thisMonthPayments.Sum(payment => payment.Amount),
            SuccessfulPaymentCount = payments.Count,
            TotalCreditsPaid = thisMonthPayments.Sum(payment => payment.CreditAmount + payment.BonusCredit),
            RevenueToday = payments
                .Where(payment => payment.PaidDate >= todayStart && payment.PaidDate < tomorrowStart)
                .Sum(payment => payment.Amount),
            MonthlyRevenue = monthlyRevenue,
            PackageSales = packageSales,
        });
    }
}
