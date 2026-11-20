using Support.API.DTOs.Auth;

namespace Support.API.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);

    Task<AuthResponse> LoginAsync(LoginRequest request);

    Task<UserResponse?> GetCurrentUserAsync(int userId);
}