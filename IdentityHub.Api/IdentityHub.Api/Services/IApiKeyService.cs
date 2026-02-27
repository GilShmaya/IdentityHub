using IdentityHub.Api.DTOs;

namespace IdentityHub.Api.Services;

public interface IApiKeyService
{
    Task<ApiKeyCreatedResponse> CreateKeyAsync(int userId, CreateApiKeyRequest request);
    Task<IEnumerable<ApiKeyResponse>> GetKeysAsync(int userId);
    Task RevokeKeyAsync(int userId, int keyId);
    Task<int?> ValidateKeyAsync(string rawKey);
}
