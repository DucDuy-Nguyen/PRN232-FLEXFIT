using Flexfit.Models;
using Microsoft.EntityFrameworkCore;

namespace Flexfit.Repositories
{
    public class CreditRepository : ICreditRepository
    {
        private readonly FlexFitDbContext _db;

        public CreditRepository(FlexFitDbContext db)
        {
            _db = db;
        }

        public async Task<IEnumerable<CreditPackage>> GetAllPackagesAsync()
        {
            return await _db.CreditPackages.OrderByDescending(p => p.CreatedAt).ToListAsync();
        }

        public async Task<CreditPackage?> GetPackageByIdAsync(Guid id)
        {
            return await _db.CreditPackages.FindAsync(id);
        }

        public async Task AddPackageAsync(CreditPackage package)
        {
            await _db.CreditPackages.AddAsync(package);
            await _db.SaveChangesAsync();
        }

        public async Task UpdatePackageAsync(CreditPackage package)
        {
            _db.CreditPackages.Update(package);
            await _db.SaveChangesAsync();
        }

        public async Task DeletePackageAsync(Guid id)
        {
            var package = await _db.CreditPackages.FindAsync(id);
            if (package != null)
            {
                _db.CreditPackages.Remove(package);
                await _db.SaveChangesAsync();
            }
        }
        // CreditTransaction
        public async Task<UserCredit?> GetUserCreditByUserIdAsync(Guid userId)
        {
            return await _db.UserCredits.FirstOrDefaultAsync(uc => uc.UserId == userId);
        }

        public async Task AddUserCreditAsync(UserCredit userCredit)
        {
            await _db.UserCredits.AddAsync(userCredit);
            await _db.SaveChangesAsync();
        }

        public async Task UpdateUserCreditAsync(UserCredit userCredit)
        {
            _db.UserCredits.Update(userCredit);
            await _db.SaveChangesAsync();
        }

        public async Task<IEnumerable<CreditTransaction>> GetTransactionsByUserIdAsync(Guid userId)
        {
            return await _db.CreditTransactions
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.CreatedAt) // Mới nhất xếp lên đầu
                .ToListAsync();
        }

        public async Task AddTransactionAsync(CreditTransaction transaction)
        {
            await _db.CreditTransactions.AddAsync(transaction);
            await _db.SaveChangesAsync();
        }
    }
}