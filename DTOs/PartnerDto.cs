using System;
using System.Collections.Generic;

namespace Flexfit.DTOs
{
    public class PartnerDashboardDto
    {
        public decimal RevenueThisMonth { get; set; }
        public int BookingsThisMonth { get; set; }
        public int NewCustomersThisMonth { get; set; }
        public double OccupancyRate { get; set; }
        public int TotalClasses { get; set; }
        public int TotalBranches { get; set; }
        public List<MonthlyMetricDto> RevenueChart { get; set; } = new();
        public List<MonthlyMetricDto> BookingChart { get; set; } = new();
    }

    public class MonthlyMetricDto
    {
        public string Month { get; set; } = string.Empty;
        public decimal Value { get; set; }
    }

    public class PartnerCustomerDto
    {
        public Guid UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public int TotalBookings { get; set; }
        public int TotalCreditUsed { get; set; }
        public DateTime? LastBookingAt { get; set; }
    }

    public class PartnerRevenueDto
    {
        public decimal TotalRevenue { get; set; }
        public decimal RevenueThisMonth { get; set; }
        public decimal RevenueThisWeek { get; set; }
        public decimal RevenueThisYear { get; set; }
        public string? TopClassName { get; set; }
        public string? TopBranchName { get; set; }
        public List<NameTotalDto> RevenueByBranch { get; set; } = new();
        public List<NameTotalDto> RevenueByClass { get; set; } = new();
        public List<NameTotalDto> RevenueByMonth { get; set; } = new();
    }

    public class NameTotalDto
    {
        public string? Name { get; set; }
        public decimal Total { get; set; }
    }
}
