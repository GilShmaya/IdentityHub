using System.ComponentModel.DataAnnotations;

namespace IdentityHub.Api.Models;

public class JiraConfiguration
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    [Required, MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string EncryptedApiToken { get; set; } = string.Empty;

    [Required, MaxLength(512)]
    public string SiteUrl { get; set; } = string.Empty;
}
