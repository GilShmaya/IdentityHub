using System.ComponentModel.DataAnnotations;

namespace IdentityHub.Api.DTOs;

/// <summary>Request payload for creating a new API key.</summary>
public record CreateApiKeyRequest(
    [Required, MaxLength(128)] string Name,
    [Required, Range(1, 365)] int ExpiresInDays
);

/// <summary>Response containing the newly created API key (shown only once).</summary>
public record CreateApiKeyResponse(
    int Id,
    string Name,
    string Key,
    string Prefix,
    DateTime CreatedAt,
    DateTime ExpiresAt
);

/// <summary>API key metadata returned when listing keys (excludes the raw key).</summary>
public record ApiKeyResponse(
    int Id,
    string Name,
    string Prefix,
    DateTime CreatedAt,
    DateTime ExpiresAt,
    bool IsRevoked,
    bool IsExpired
);
