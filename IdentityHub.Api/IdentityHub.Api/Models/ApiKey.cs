using System.ComponentModel.DataAnnotations;

namespace IdentityHub.Api.Models;

/// <summary>Represents an API key used for external REST API authentication.</summary>
public class ApiKey
{
    public int Id { get; set; }

    public int UserId { get; set; }

    [Required, MaxLength(128)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string KeyHash { get; set; } = string.Empty;

    [Required, MaxLength(16)]
    public string KeyPrefix { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsRevoked { get; set; }

    public DateTime ExpiresAt { get; set; }

    public User User { get; set; } = null!;
}
