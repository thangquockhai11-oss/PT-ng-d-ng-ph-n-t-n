using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

using Support.API.Data;
using Support.API.DTOs.Auth;
using Support.API.Interfaces;

using Support.Shared.Enums;
using Support.Shared.Models;

namespace Support.API.Services.Auth;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly PasswordHasher<User> _passwordHasher;

    public AuthService(
        ApplicationDbContext context,
        IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;

        _passwordHasher = new PasswordHasher<User>();
    }

    // =========================
    // REGISTER
    // =========================
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        // Validate dữ liệu
        if (string.IsNullOrWhiteSpace(request.FullName))
        {
            throw new InvalidOperationException("Họ tên không được để trống.");
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            throw new InvalidOperationException("Email không được để trống.");
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            throw new InvalidOperationException("Mật khẩu không được để trống.");
        }

        if (request.Password.Length < 8)
        {
            throw new InvalidOperationException(
                "Mật khẩu phải có ít nhất 8 ký tự.");
        }

        string email = request.Email.Trim().ToLowerInvariant();

        // Kiểm tra email tồn tại
        bool emailExists = await _context.Users
            .AnyAsync(x => x.Email.ToLower() == email);

        if (emailExists)
        {
            throw new InvalidOperationException(
                "Email này đã được đăng ký.");
        }

        // Tạo User
        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = email,
            Role = UserRole.Customer,
            CreatedAt = DateTime.UtcNow
        };

        // Hash mật khẩu
        user.PasswordHash = _passwordHasher.HashPassword(
            user,
            request.Password
        );

        // Lưu database
        _context.Users.Add(user);

        await _context.SaveChangesAsync();

        // Tạo Token
        return GenerateToken(user);
    }

    // =========================
    // LOGIN
    // =========================
    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            throw new InvalidOperationException(
                "Email và mật khẩu không được để trống.");
        }

        string email = request.Email.Trim().ToLowerInvariant();

        // Tìm User
        var user = await _context.Users
            .FirstOrDefaultAsync(x => x.Email.ToLower() == email);

        if (user == null)
        {
            throw new InvalidOperationException(
                "Email hoặc mật khẩu không chính xác.");
        }

        // Kiểm tra mật khẩu
        var result = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            request.Password
        );

        if (result == PasswordVerificationResult.Failed)
        {
            throw new InvalidOperationException(
                "Email hoặc mật khẩu không chính xác.");
        }

        // Sinh Token
        return GenerateToken(user);
    }

    // =========================
    // GET CURRENT USER
    // =========================
    public async Task<UserResponse?> GetCurrentUserAsync(int userId)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(x => x.Id == userId);

        if (user == null)
        {
            return null;
        }

        return new UserResponse
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role.ToString()
        };
    }

    // =========================
    // GENERATE JWT
    // =========================
    private AuthResponse GenerateToken(User user)
    {
        var jwtSettings = _configuration.GetSection("Jwt");

        string key = jwtSettings["Key"]
            ?? throw new InvalidOperationException(
                "JWT Key chưa được cấu hình.");

        string issuer = jwtSettings["Issuer"]
            ?? throw new InvalidOperationException(
                "JWT Issuer chưa được cấu hình.");

        string audience = jwtSettings["Audience"]
            ?? throw new InvalidOperationException(
                "JWT Audience chưa được cấu hình.");

        int expiresInMinutes = int.Parse(
            jwtSettings["ExpiresInMinutes"] ?? "60"
        );

        DateTime expiresAt = DateTime.UtcNow.AddMinutes(
            expiresInMinutes
        );

        var claims = new List<Claim>
        {
            new Claim(
                ClaimTypes.NameIdentifier,
                user.Id.ToString()
            ),

            new Claim(
                ClaimTypes.Name,
                user.FullName
            ),

            new Claim(
                ClaimTypes.Email,
                user.Email
            ),

            new Claim(
                ClaimTypes.Role,
                user.Role.ToString()
            )
        };

        var securityKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(key)
        );

        var credentials = new SigningCredentials(
            securityKey,
            SecurityAlgorithms.HmacSha256
        );

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials
        );

        string tokenString = new JwtSecurityTokenHandler()
            .WriteToken(token);

        return new AuthResponse
        {
            Token = tokenString,
            ExpiresAt = expiresAt,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role.ToString()
        };
    }
}