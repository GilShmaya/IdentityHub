using IdentityHub.Api.DTOs;

namespace IdentityHub.Api.Services;

/// <summary>Contract for user registration and login operations.</summary>
public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
}
