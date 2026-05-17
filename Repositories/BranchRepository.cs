using Flexfit.Models;
using Microsoft.EntityFrameworkCore;

namespace Flexfit.Repositories
{
    public class BranchRepository : IBranchRepository
    {
        private readonly FlexFitDbContext _db;
        public BranchRepository(FlexFitDbContext db) => _db = db;

        // 1. Sửa hàm GetAllAsync: Tải kèm dữ liệu nhân viên thông qua bảng trung gian
        public async Task<IEnumerable<Branch>> GetAllAsync() =>
            await _db.Branches
                .Include(b => b.BranchStaffs)          // Đi vào bảng trung gian BranchStaffs
                    .ThenInclude(bs => bs.Staff)       // Đi tiếp vào bảng Users để lấy thông tin nhân viên
                .ToListAsync();

        // 2. Sửa hàm GetByIdAsync: Tải kèm dữ liệu nhân viên tương tự cho một chi nhánh cụ thể
        public async Task<Branch?> GetByIdAsync(Guid id) =>
            await _db.Branches
                .Include(b => b.BranchStaffs)          // Đi vào bảng trung gian BranchStaffs
                    .ThenInclude(bs => bs.Staff)       // Đi tiếp vào bảng Users
                .FirstOrDefaultAsync(b => b.BranchId == id); // Dùng FirstOrDefaultAsync thay cho FindAsync khi có Include

        public async Task AddAsync(Branch branch)
        {
            await _db.Branches.AddAsync(branch);
            await _db.SaveChangesAsync();
        }

        public async Task UpdateAsync(Branch branch)
        {
            _db.Branches.Update(branch);
            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var branch = await _db.Branches.FindAsync(id);
            if (branch != null)
            {
                _db.Branches.Remove(branch);
                await _db.SaveChangesAsync();
            }
        }
    }
}