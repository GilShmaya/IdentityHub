using System.ComponentModel.DataAnnotations;

namespace IdentityHub.Api.DTOs;

public record CreateApiKeyRequest(
    [Required, MaxLength(128)] string Name
);

public record ApiKeyResponse(int Id, string Name, string KeyPrefix, DateTime CreatedAt, bool IsRevoked);

public record ApiKeyCreatedResponse(int Id, string Name, string Key, DateTime CreatedAt);
