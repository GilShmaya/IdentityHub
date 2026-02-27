using IdentityHub.Api.DTOs;
using IdentityHub.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityHub.Api.Controllers;

[Route("api/keys")]
[Authorize]
public class ApiKeysController : BaseController
{
    private readonly IApiKeyService _apiKeyService;

    public ApiKeysController(IApiKeyService apiKeyService)
    {
        _apiKeyService = apiKeyService;
    }

    [HttpPost]
    public async Task<ActionResult<ApiKeyCreatedResponse>> CreateKey([FromBody] CreateApiKeyRequest request)
    {
        var result = await _apiKeyService.CreateKeyAsync(GetUserId(), request);
        return CreatedAtAction(nameof(GetKeys), result);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ApiKeyResponse>>> GetKeys()
    {
        var keys = await _apiKeyService.GetKeysAsync(GetUserId());
        return Ok(keys);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> RevokeKey(int id)
    {
        await _apiKeyService.RevokeKeyAsync(GetUserId(), id);
        return NoContent();
    }
}
