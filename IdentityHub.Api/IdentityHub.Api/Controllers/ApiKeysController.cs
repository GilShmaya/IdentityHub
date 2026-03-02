using IdentityHub.Api.DTOs;
using IdentityHub.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityHub.Api.Controllers;

/// <summary>API key management endpoints for creation, listing, and revocation.</summary>
[Route("api/keys")]
[ApiController]
[Authorize]
public class ApiKeysController : BaseController
{
    private readonly IApiKeyService _apiKeyService;

    public ApiKeysController(IApiKeyService apiKeyService)
    {
        _apiKeyService = apiKeyService;
    }

    /// <summary>
    /// Create a new API key for the authenticated user.
    /// </summary>
    /// <remarks>
    /// The raw key is returned only once. Store it securely — it cannot be retrieved again.
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(CreateApiKeyResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateApiKeyRequest request)
    {
        var result = await _apiKeyService.CreateAsync(GetUserId(), request.Name, request.ExpiresInDays);
        return StatusCode(201, result);
    }

    /// <summary>
    /// List all API keys for the authenticated user.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ApiKeyResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List()
    {
        var keys = await _apiKeyService.ListByUserAsync(GetUserId());
        return Ok(keys);
    }

    /// <summary>
    /// Revoke an API key.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Revoke(int id)
    {
        await _apiKeyService.RevokeAsync(GetUserId(), id);
        return NoContent();
    }
}
