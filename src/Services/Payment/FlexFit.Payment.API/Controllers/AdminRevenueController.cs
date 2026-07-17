using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FlexFit.Payment.Application.DTOs.AdminRevenue;
using FlexFit.Payment.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlexFit.Payment.API.Controllers
{
    [ApiController]
    [Route("api/admin/revenue")]
    [Authorize(Roles = "Admin")]
    public class AdminRevenueController : ControllerBase
    {
        private static readonly string[] SuccessfulStatuses = { "Paid", "Success", "Completed" };
        private readonly IPaymentRepository _paymentRepository;
        private readonly ICacheService _cacheService;

        public AdminRevenueController(IPaymentRepository paymentRepository, ICacheService cacheService)
        {
            _paymentRepository = paymentRepository;
            _cacheService = cacheService;
        }

        [HttpGet("summary")]
        public async Task<ActionResult<AdminRevenueSummaryResponse>> GetSummary()
        {
            var cacheKey = "payment:admin:revenue_summary";
            var cached = await _cacheService.GetAsync<AdminRevenueSummaryResponse>(cacheKey);
            if (cached != null)
            {
                return Ok(cached);
            }

            var now = DateTime.UtcNow; // Standard UTC operations
            var vnTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            var vnNow = TimeZoneInfo.ConvertTimeFromUtc(now, vnTimeZone);

            var todayStart = vnNow.Date;
            var tomorrowStart = todayStart.AddDays(1);
            var monthStart = new DateTime(vnNow.Year, vnNow.Month, 1);
            var nextMonthStart = monthStart.AddMonths(1);
            var chartStart = monthStart.AddMonths(-5);

            var allPayments = await _paymentRepository.GetAllPaymentsAsync();
            var successfulPayments = allPayments
                .Where(p => SuccessfulStatuses.Contains(p.Status))
                .ToList();

            // Total revenue this month
            var totalRevenueThisMonth = successfulPayments
                .Where(p => {
                    var paidAtLocal = TimeZoneInfo.ConvertTimeFromUtc(p.PaidAt ?? p.CreatedAt, vnTimeZone);
                    return paidAtLocal >= monthStart && paidAtLocal < nextMonthStart;
                })
                .Sum(p => p.Amount);

            var successfulPaymentCount = successfulPayments.Count;

            var totalCreditsPaid = successfulPayments
                .Where(p => {
                    var paidAtLocal = TimeZoneInfo.ConvertTimeFromUtc(p.PaidAt ?? p.CreatedAt, vnTimeZone);
                    return paidAtLocal >= monthStart && paidAtLocal < nextMonthStart;
                })
                .Sum(p => p.Package != null ? (p.Package.CreditAmount + p.Package.BonusCredit) : 0);

            var revenueToday = successfulPayments
                .Where(p => {
                    var paidAtLocal = TimeZoneInfo.ConvertTimeFromUtc(p.PaidAt ?? p.CreatedAt, vnTimeZone);
                    return paidAtLocal >= todayStart && paidAtLocal < tomorrowStart;
                })
                .Sum(p => p.Amount);

            // Last 6 months trend
            var rawMonthlyRevenue = successfulPayments
                .Where(p => {
                    var paidAtLocal = TimeZoneInfo.ConvertTimeFromUtc(p.PaidAt ?? p.CreatedAt, vnTimeZone);
                    return paidAtLocal >= chartStart;
                })
                .GroupBy(p => {
                    var paidAtLocal = TimeZoneInfo.ConvertTimeFromUtc(p.PaidAt ?? p.CreatedAt, vnTimeZone);
                    return new { paidAtLocal.Year, paidAtLocal.Month };
                })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    Revenue = g.Sum(p => p.Amount)
                })
                .ToList();

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

            // Group sales by package
            var packageSales = successfulPayments
                .Where(p => {
                    var paidAtLocal = TimeZoneInfo.ConvertTimeFromUtc(p.PaidAt ?? p.CreatedAt, vnTimeZone);
                    return paidAtLocal >= monthStart && paidAtLocal < nextMonthStart;
                })
                .GroupBy(p => p.Package?.PackageName ?? "Gói đã xóa")
                .Select(g => new PackageSalesItem
                {
                    PackageName = g.Key,
                    Count = g.Count(),
                    Revenue = g.Sum(p => p.Amount),
                })
                .OrderByDescending(item => item.Revenue)
                .ThenBy(item => item.PackageName)
                .ToList();

            var summaryResponse = new AdminRevenueSummaryResponse
            {
                TotalRevenueThisMonth = totalRevenueThisMonth,
                SuccessfulPaymentCount = successfulPaymentCount,
                TotalCreditsPaid = totalCreditsPaid,
                RevenueToday = revenueToday,
                MonthlyRevenue = monthlyRevenue,
                PackageSales = packageSales,
            };

            await _cacheService.SetAsync(cacheKey, summaryResponse, TimeSpan.FromMinutes(10));

            return Ok(summaryResponse);
        }
    }
}
