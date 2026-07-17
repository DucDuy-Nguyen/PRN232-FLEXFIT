using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FlexFit.Payment.Application.Interfaces;
using FlexFit.Payment.Domain.Entities;
using FlexFit.Payment.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace FlexFit.Payment.Infrastructure.Repositories
{
    public class CreditRepository : ICreditRepository
    {
        private readonly PaymentDbContext _context;
        private IDbContextTransaction? _currentTransaction;

        public CreditRepository(PaymentDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<CreditPackage>> GetAllPackagesAsync()
        {
            return await _context.CreditPackages.OrderByDescending(p => p.CreatedAt).ToListAsync();
        }

        public async Task<CreditPackage?> GetPackageByIdAsync(Guid id)
        {
            return await _context.CreditPackages.FindAsync(id);
        }

        public async Task AddPackageAsync(CreditPackage package)
        {
            await _context.CreditPackages.AddAsync(package);
            await _context.SaveChangesAsync();
        }

        public async Task UpdatePackageAsync(CreditPackage package)
        {
            _context.CreditPackages.Update(package);
            await _context.SaveChangesAsync();
        }

        public async Task DeletePackageAsync(Guid id)
        {
            var package = await _context.CreditPackages.FindAsync(id);
            if (package != null)
            {
                _context.CreditPackages.Remove(package);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<UserCredit?> GetUserCreditByUserIdAsync(Guid userId)
        {
            return await _context.UserCredits.FirstOrDefaultAsync(uc => uc.UserId == userId);
        }

        public async Task AddUserCreditAsync(UserCredit userCredit)
        {
            await _context.UserCredits.AddAsync(userCredit);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateUserCreditAsync(UserCredit userCredit)
        {
            _context.UserCredits.Update(userCredit);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<CreditTransaction>> GetTransactionsByUserIdAsync(Guid userId)
        {
            return await _context.CreditTransactions
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task AddTransactionAsync(CreditTransaction transaction)
        {
            await _context.CreditTransactions.AddAsync(transaction);
            await _context.SaveChangesAsync();
        }

        public async Task BeginTransactionAsync()
        {
            _currentTransaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            if (_currentTransaction != null)
            {
                await _currentTransaction.CommitAsync();
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_currentTransaction != null)
            {
                await _currentTransaction.RollbackAsync();
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
            }
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
