using System.ComponentModel.DataAnnotations;

namespace IdentityHub.Api.DTOs;

public record CreateApiKeyRequest(
    [Required, MaxLength(128)] string Name
);

public record CreateApiKeyResponse(
    int Id,
    string Name,
    string Key,
    string Prefix,
    DateTime CreatedAt
);

public record ApiKeyResponse(
    int Id,
    string Name,
    string Prefix,
    DateTime CreatedAt,
    bool IsRevoked
);
