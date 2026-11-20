using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using System.Security.Claims;

using Support.API.DTOs.Auth;
using Support.API.Interfaces;

namespace Support.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    // =========================
    // REGISTER
    // POST: api/auth/register
    // =========================
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request)
    {
        try
        {
            var result = await _authService.RegisterAsync(request);

            return StatusCode(StatusCodes.Status201Created, new
            {
                message = "Đăng ký thành công.",
                data = result
            });
        }
        catch (InvalidOperationException ex)
        {
            var response = new
            {
                message = ex.Message
            };
            return ex.Message.Contains("đã được đăng ký", StringComparison.OrdinalIgnoreCase)
                ? Conflict(response)
                : BadRequest(response);
        }
    }

    // =========================
    // LOGIN
    // POST: api/auth/login
    // =========================
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request)
    {
        try
        {
            var result = await _authService.LoginAsync(request);

            return Ok(new
            {
                message = "Đăng nhập thành công.",
                data = result
            });
        }
        catch (InvalidOperationException ex)
        {
            return Unauthorized(new
            {
                message = ex.Message
            });
        }
    }

    // =========================
    // GET CURRENT USER
    // GET: api/auth/me
    // =========================
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userIdString = User.FindFirstValue(
            ClaimTypes.NameIdentifier
        );

        if (!int.TryParse(userIdString, out int userId))
        {
            return Unauthorized(new
            {
                message = "Token không hợp lệ."
            });
        }

        var user = await _authService.GetCurrentUserAsync(userId);

        if (user == null)
        {
            return NotFound(new
            {
                message = "Không tìm thấy người dùng."
            });
        }

        return Ok(new
        {
            message = "Lấy thông tin người dùng thành công.",
            data = user
        });
    }
}
