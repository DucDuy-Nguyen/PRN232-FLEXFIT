using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FlexFit.Payment.API.Contracts.Requests.CreditPackages;
using FlexFit.Payment.API.Contracts.Responses.CreditPackages;
using FlexFit.Payment.API.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlexFit.Payment.API.Controllers
{
    [ApiController]
    [Route("api/credit-packages")]
    public class CreditPackagesController : ControllerBase
    {
        private readonly ICreditService _creditService;

        public CreditPackagesController(ICreditService creditService)
        {
            _creditService = creditService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllPackages()
        {
            var responses = await _creditService.GetAllPackagesAsync();
            return Ok(responses);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPackageById(Guid id)
        {
            var response = await _creditService.GetPackageByIdAsync(id);
            if (response == null) 
                return NotFound(new { message = "Không tìm thấy gói nạp." });
            return Ok(response);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> CreatePackage([FromBody] CreateCreditPackageRequest request)
        {
            var packageId = await _creditService.CreatePackageAsync(request);
            return Ok(new { message = "Tạo gói nạp Credit thành công!", packageId });
        }

        [Authorize(Roles = "Admin")]
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

        [Authorize(Roles = "Admin")]
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

        [Authorize(Roles = "Admin")]
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

        [Authorize(Roles = "Admin")]
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

        [Authorize(Roles = "Admin")]
        [HttpPost("{id}/buy")]
        public async Task<IActionResult> BuyPackage(Guid id, [FromBody] BuyCreditPackageRequest request)
        {
            try
            {
                await _creditService.BuyPackageAsync(id, request);
                return Ok(new { message = "Giao dịch thành công! Tài khoản đã được cộng Credit (Mock Buy)." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [Authorize(Roles = "Admin")]
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
        }
    }
}
