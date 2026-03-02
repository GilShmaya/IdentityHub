using IdentityHub.Api.DTOs;
using IdentityHub.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace IdentityHub.Api.Controllers;

[Route("api/[controller]")]
[EnableRateLimiting("auth")]
public class AuthController : BaseController
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Register a new user account.
    /// </summary>
    /// <remarks>
    /// Route: POST /api/auth/register
    ///
    /// Password must be at least 8 characters and contain an uppercase letter, a lowercase letter, and a digit.
    /// </remarks>
    /// <param name="request">User registration details (email and password).</param>
    /// <returns>JWT token and user email.</returns>
    /// <response code="200">Account created successfully. Returns JWT token.</response>
    /// <response code="400">Invalid input or password does not meet complexity requirements.</response>
    /// <response code="409">A user with this email already exists.</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        var result = await _authService.RegisterAsync(request);
        return Ok(result);
    }

    /// <summary>
    /// Authenticate an existing user and obtain a JWT token.
    /// </summary>
    /// <remarks>
    /// Route: POST /api/auth/login
    /// </remarks>
    /// <param name="request">User login credentials (email and password).</param>
    /// <returns>JWT token and user email.</returns>
    /// <response code="200">Login successful. Returns JWT token.</response>
    /// <response code="401">Invalid email or password.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);
        return Ok(result);
    }
}
