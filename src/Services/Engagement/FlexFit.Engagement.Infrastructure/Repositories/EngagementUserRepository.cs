using FlexFit.Engagement.Application.Interfaces;
using FlexFit.Engagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FlexFit.Engagement.Infrastructure.Repositories
{
    public class EngagementUserRepository : IEngagementUserRepository
    {
        private readonly EngagementDbContext _context;

        public EngagementUserRepository(EngagementDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Guid>> GetAllUserIdsAsync()
        {
            return await _context.Users.Select(u => u.UserId).ToListAsync();
        }
    }
}
