using Flexfit.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace Flexfit.Repositories
{
    public class MemberProfileRepository : IMemberProfileRepository
    {
        private readonly FlexFitDbContext _context;

        public MemberProfileRepository(FlexFitDbContext context)
        {
            _context = context;
        }

        public async Task<MemberProfile?> GetByUserIdAsync(Guid userId)
        {
            return await _context.MemberProfiles
                .Include(mp => mp.User) // Gộp thông tin tài khoản hội viên
                .FirstOrDefaultAsync(mp => mp.UserId == userId);
        }

        public async Task<User?> GetUserByIdAsync(Guid userId)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
        }

        public async Task AddProfileAsync(MemberProfile profile)
        {
            await _context.MemberProfiles.AddAsync(profile);
        }

        public Task UpdateProfileAsync(MemberProfile profile)
        {
            _context.MemberProfiles.Update(profile);
            return Task.CompletedTask;
        }

        public Task UpdateUserAsync(User user)
        {
            _context.Users.Update(user);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}