namespace Flexfit.Controllers
{
    using Flexfit.Service;
    using Microsoft.AspNetCore.Mvc;
    using Flexfit.DTOs;

    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;

        public AuthController(AuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var result = await _authService.RegisterAsync(request);
            return Ok(result);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var result = await _authService.LoginAsync(request);
            return Ok(result);
        }
        [HttpPost("verify-email")]
        public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request)
        {
            // Gọi thẳng hàm từ class AuthService
            var result = await _authService.VerifyEmailAsync(request.Email, request.OtpCode);

            if (result == "Xác thực tài khoản thành công!")
            {
                return Ok(new { message = result });
            }

            return BadRequest(new { message = result });
        }
    }
}
