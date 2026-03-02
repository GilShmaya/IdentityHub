using System.ComponentModel.DataAnnotations;

namespace IdentityHub.Api.DTOs;

/// <summary>Request payload for user registration.</summary>
public record RegisterRequest(
    [Required, EmailAddress, MaxLength(256)] string Email,
    [Required, MinLength(8), MaxLength(128)] string Password
);

/// <summary>Request payload for user login.</summary>
public record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password
);

/// <summary>Response containing a JWT token and user email after authentication.</summary>
public record AuthResponse(string Token, string Email);
