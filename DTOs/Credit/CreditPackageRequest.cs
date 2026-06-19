namespace Flexfit.DTOs.Credit
{
    public class CreateCreditPackageRequest
    {
        public string PackageName { get; set; } = null!;
        public int CreditAmount { get; set; }
        public decimal Price { get; set; }
        public string? Description { get; set; }
    }

    // Khi Admin cập nhật gói nạp
    public class UpdateCreditPackageRequest
    {
        public string? PackageName { get; set; }
        public int? CreditAmount { get; set; }
        public decimal? Price { get; set; }
        public string? Description { get; set; }
    }

    // Khi User nhấn mua gói nạp
    public class BuyCreditPackageRequest
    {
        public Guid UserId { get; set; }
    }
    public class AdminAddCreditRequest
    {
        public Guid UserId { get; set; }
        public int Amount { get; set; }
        public string? Description { get; set; } // Lý do cộng (ví dụ: "Tặng quà sự kiện,refund")
    }
}