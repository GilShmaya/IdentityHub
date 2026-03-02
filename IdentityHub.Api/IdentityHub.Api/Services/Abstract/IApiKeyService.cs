using IdentityHub.Api.DTOs;

namespace IdentityHub.Api.Services;

/// <summary>Contract for API key creation, validation, and revocation.</summary>
public interface IApiKeyService
{
    Task<CreateApiKeyResponse> CreateAsync(int userId, string name, int expiresInDays);
    Task<int?> ValidateAsync(string rawKey);
    Task RevokeAsync(int userId, int keyId);
    Task<IEnumerable<ApiKeyResponse>> ListByUserAsync(int userId);
}
