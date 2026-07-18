using System;

namespace FlexFit.Payment.API.Contracts.Requests.CreditPackages
{
    public class CreateCreditPackageRequest
    {
        public string PackageName { get; set; } = null!;
        public int CreditAmount { get; set; }
        public decimal Price { get; set; }
        public string? Description { get; set; }
    }

    public class UpdateCreditPackageRequest
    {
        public string? PackageName { get; set; }
        public int? CreditAmount { get; set; }
        public decimal? Price { get; set; }
        public string? Description { get; set; }
    }

    public class BuyCreditPackageRequest
    {
        public Guid UserId { get; set; }
    }

    public class AdminAddCreditRequest
    {
        public Guid UserId { get; set; }
        public int Amount { get; set; }
        public string? Description { get; set; }
    }
}
