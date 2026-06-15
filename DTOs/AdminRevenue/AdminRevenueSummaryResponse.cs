namespace Flexfit.DTOs.AdminRevenue;

public class AdminRevenueSummaryResponse
{
    public decimal TotalRevenueThisMonth { get; set; }
    public int SuccessfulPaymentCount { get; set; }
    public int TotalCreditsPaid { get; set; }
    public decimal RevenueToday { get; set; }
    public List<MonthlyRevenueItem> MonthlyRevenue { get; set; } = new();
    public List<PackageSalesItem> PackageSales { get; set; } = new();
}

public class MonthlyRevenueItem
{
    public string Month { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
}

public class PackageSalesItem
{
    public string PackageName { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Revenue { get; set; }
}
