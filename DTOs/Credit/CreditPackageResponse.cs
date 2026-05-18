namespace Flexfit.DTOs.Credit
{
    // 1. Response trả về thông tin gói nạp Credit
    public class CreditPackageResponse
    {
        public Guid PackageId { get; set; }
        public string PackageName { get; set; } = null!;
        public int CreditAmount { get; set; }
        public decimal Price { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public bool IsPopular { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // 2. Response trả về chi tiết lịch sử giao dịch (Biến động số dư)
    public class CreditTransactionResponse
    {
        public Guid TransactionId { get; set; }
        public Guid UserId { get; set; }
        public int Amount { get; set; }
        public int BalanceBefore { get; set; }
        public int BalanceAfter { get; set; }
        public string Type { get; set; } = null!;
        public Guid? ReferenceId { get; set; }
        public string? ReferenceType { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
    }
    public class UserCreditResponse
    {
        public Guid UserCreditId { get; set; }
        public Guid UserId { get; set; }
        public int Balance { get; set; }
        public int TotalEarned { get; set; }
        public int TotalSpent { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}