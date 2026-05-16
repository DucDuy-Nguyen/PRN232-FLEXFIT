using Flexfit.DTOs;
using Flexfit.Models;
using Flexfit.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Flexfit.Controllers
{
    [Route("api/branches")]
    [ApiController]
    public class BranchController : ControllerBase
    {
        private readonly IBranchRepository _branchRepo;
        public BranchController(IBranchRepository branchRepo) => _branchRepo = branchRepo;

        [HttpGet]
        public async Task<IActionResult> GetAllBranches()
        {
            var branches = await _branchRepo.GetAllAsync();
            var dtos = branches.Select(b => new BranchDto
            {
                BranchId = b.BranchId,
                GymId = b.GymId,
                BranchName = b.BranchName,
                Address = b.Address,
                City = b.City,
                District = b.District,
                OpenTime = b.OpenTime,
                CloseTime = b.CloseTime,
                ThumbnailUrl = b.ThumbnailUrl,
                IsActive = b.IsActive,
                CreatedAt = b.CreatedAt
            });
            return Ok(dtos);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetBranchById(Guid id)
        {
            var b = await _branchRepo.GetByIdAsync(id);
            if (b == null) return NotFound(new { message = "Không tìm thấy chi nhánh." });

            return Ok(new BranchDto
            {
                BranchId = b.BranchId,
                GymId = b.GymId,
                BranchName = b.BranchName,
                Address = b.Address,
                City = b.City,
                District = b.District,
                OpenTime = b.OpenTime,
                CloseTime = b.CloseTime,
                ThumbnailUrl = b.ThumbnailUrl,
                IsActive = b.IsActive,
                CreatedAt = b.CreatedAt
            });
        }

        [HttpPost]
        public async Task<IActionResult> CreateBranch(CreateBranchRequest request)
        {
            var newBranch = new Branch
            {
                BranchId = Guid.NewGuid(),
                GymId = request.GymId,
                BranchName = request.BranchName,
                Address = request.Address,
                City = request.City,
                District = request.District,
                OpenTime = request.OpenTime,
                CloseTime = request.CloseTime,
                ThumbnailUrl = request.ThumbnailUrl,
                IsActive = true, // Mặc định mở cửa
                CreatedAt = DateTime.UtcNow
            };

            await _branchRepo.AddAsync(newBranch);
            return Ok(new { message = "Tạo chi nhánh thành công!", branchId = newBranch.BranchId });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBranch(Guid id, UpdateBranchRequest request)
        {
            var branch = await _branchRepo.GetByIdAsync(id);
            if (branch == null) return NotFound(new { message = "Không tìm thấy chi nhánh." });

            branch.BranchName = request.BranchName;
            branch.Address = request.Address;
            branch.City = request.City;
            branch.District = request.District;
            branch.OpenTime = request.OpenTime;
            branch.CloseTime = request.CloseTime;
            branch.ThumbnailUrl = request.ThumbnailUrl;
            branch.UpdatedAt = DateTime.UtcNow;

            await _branchRepo.UpdateAsync(branch);
            return Ok(new { message = "Cập nhật thông tin chi nhánh thành công!" });
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> ChangeBranchStatus(Guid id, [FromBody] bool isActive)
        {
            var branch = await _branchRepo.GetByIdAsync(id);
            if (branch == null) return NotFound(new { message = "Không tìm thấy chi nhánh." });

            branch.IsActive = isActive;
            branch.UpdatedAt = DateTime.UtcNow;

            await _branchRepo.UpdateAsync(branch);
            string statusMsg = isActive ? "Hoạt động" : "Tạm ngưng";
            return Ok(new { message = $"Đã chuyển trạng thái chi nhánh thành: {statusMsg}" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBranch(Guid id)
        {
            var branch = await _branchRepo.GetByIdAsync(id);
            if (branch == null) return NotFound(new { message = "Không tìm thấy chi nhánh." });

            await _branchRepo.DeleteAsync(id);
            return Ok(new { message = "Xóa chi nhánh thành công!" });
        }
    }
}