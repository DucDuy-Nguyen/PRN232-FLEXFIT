using Flexfit.DTOs;
using Flexfit.Services;
using Microsoft.AspNetCore.Mvc;

namespace Flexfit.Controllers
{
    [Route("api/users")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
            var dtos = await _userService.GetAllUsersAsync();
            return Ok(dtos);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(Guid id)
        {
            var dto = await _userService.GetUserByIdAsync(id);
            if (dto == null) return NotFound(new { message = "Không tìm thấy người dùng." });
            return Ok(dto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserRequest request)
        {
            try
            {
                await _userService.UpdateUserAsync(id, request);
                return Ok(new { message = "Cập nhật thông tin cá nhân thành công!" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> ChangeUserStatus(Guid id, [FromBody] bool isActive)
        {
            try
            {
                await _userService.ChangeUserStatusAsync(id, isActive);
                string statusMessage = isActive ? "Mở khóa" : "Khóa";
                return Ok(new { message = $"{statusMessage} tài khoản thành công!" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            try
            {
                await _userService.DeleteUserAsync(id);
                return Ok(new { message = "Xóa người dùng thành công!" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPost("assign-role")]
        public async Task<IActionResult> AssignRole([FromBody] UserRoleRequestDto request)
        {
            try
            {
                var resultMessage = await _userService.AssignRoleAsync(request);
                return Ok(new { message = resultMessage });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("revoke-role")]
        public async Task<IActionResult> RevokeRole([FromQuery] Guid userId, [FromQuery] string roleName)
        {
            try
            {
                var resultMessage = await _userService.RevokeRoleAsync(userId, roleName);
                return Ok(new { message = resultMessage });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}