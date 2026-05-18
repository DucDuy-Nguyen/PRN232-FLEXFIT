using Flexfit.DTOs.Credit;
using Flexfit.Services;
using Microsoft.AspNetCore.Mvc;

namespace Flexfit.Controllers
{
    [Route("api/credit-packages")]
    [ApiController]
    public class CreditPackageController : ControllerBase
    {
        private readonly ICreditService _creditService;

        public CreditPackageController(ICreditService creditService)
        {
            _creditService = creditService;
        }

        // 1. Lấy danh sách gói nạp
        [HttpGet]
        public async Task<IActionResult> GetAllPackages()
        {
            var responses = await _creditService.GetAllPackagesAsync();
            return Ok(responses);
        }

        // 2. Lấy chi tiết gói nạp
        [HttpGet("{id}")]
        public async Task<IActionResult> GetPackageById(Guid id)
        {
            var response = await _creditService.GetPackageByIdAsync(id);
            if (response == null) return NotFound(new { message = "Không tìm thấy gói nạp." });
            return Ok(response);
        }

        // 3. Admin tạo gói nạp mới
        [HttpPost]
        public async Task<IActionResult> CreatePackage([FromBody] CreateCreditPackageRequest request)
        {
            var packageId = await _creditService.CreatePackageAsync(request);
            return Ok(new { message = "Tạo gói nạp Credit thành công!", packageId });
        }

        // 4. Admin sửa thông tin gói nạp
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePackage(Guid id, [FromBody] UpdateCreditPackageRequest request)
        {
            try
            {
                await _creditService.UpdatePackageAsync(id, request);
                return Ok(new { message = "Cập nhật thông tin gói nạp thành công!" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        // 5. Admin ẩn/hiện gói nạp
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> ChangePackageStatus(Guid id, [FromBody] bool isActive)
        {
            try
            {
                await _creditService.ChangePackageStatusAsync(id, isActive);
                string statusMsg = isActive ? "Hoạt động" : "Tạm dừng";
                return Ok(new { message = $"Đã chuyển trạng thái gói thành: {statusMsg}" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }
        // 5.2 Admin bật/tắt trạng thái gói nạp phổ biến (Popular)
        [HttpPatch("{id}/popular")]
        public async Task<IActionResult> ChangePackagePopularStatus(Guid id, [FromBody] bool isPopular)
        {
            try
            {
                await _creditService.ChangePackagePopularStatusAsync(id, isPopular);
                string statusMsg = isPopular ? "Được yêu thích (Popular)" : "Bình thường";
                return Ok(new { message = $"Đã chuyển trạng thái nhãn gói thành: {statusMsg}" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        // 6. Admin xóa gói nạp
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePackage(Guid id)
        {
            try
            {
                await _creditService.DeletePackageAsync(id);
                return Ok(new { message = "Xóa gói nạp thành công!" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }
        // --- Credit Transactions ---

        // 7. API Đặt mua gói nạp (RESTful Sub-resource: POST /api/credit-packages/{id}/buy)
        [HttpPost("{id}/buy")]
        public async Task<IActionResult> BuyPackage(Guid id, [FromBody] BuyCreditPackageRequest request)
        {
            try
            {
                await _creditService.BuyPackageAsync(id, request);
                return Ok(new { message = "Giao dịch thành công! Tài khoản đã được cộng Credit và hệ thống đã ghi nhận lịch sử biến động số dư." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        // 8. API Xem lịch sử giao dịch của một Người dùng cụ thể
        [HttpGet("/api/users/{userId}/credit-transactions")]
        public async Task<IActionResult> GetUserTransactionHistory(Guid userId)
        {
            var responses = await _creditService.GetUserTransactionHistoryAsync(userId);
            return Ok(responses);
        }
        // 9. API Admin chủ động cộng Credit cho một User bất kỳ
        // [Authorize(Roles = "Admin")] // Mở ra nếu hệ thống đã cấu hình JWT Token cho Admin
        [HttpPost("admin-adjustment")]
        public async Task<IActionResult> AdminAddCredit([FromBody] AdminAddCreditRequest request)
        {
            try
            {
                await _creditService.AdminAddCreditToUserAsync(request);
                return Ok(new { message = $"Đã cộng thành công {request.Amount} credits cho người dùng!" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Đã xảy ra lỗi hệ thống khi điều chỉnh số dư.", detail = ex.Message });
            }
        }
        [HttpGet("/api/users/{userId}/credit-wallet")]
        public async Task<IActionResult> GetUserCreditWallet(Guid userId)
        {
            var response = await _creditService.GetUserCreditAsync(userId);
            if (response == null)
            {
                // Trả về số dư bằng 0 ảo nếu User tồn tại nhưng chưa từng phát sinh nạp tiền
                return Ok(new
                {
                    userId = userId,
                    balance = 0,
                    totalEarned = 0,
                    totalSpent = 0,
                    updatedAt = DateTime.UtcNow
                });
            }
            return Ok(response);
        }
    }
}