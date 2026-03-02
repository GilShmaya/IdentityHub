using System.ComponentModel.DataAnnotations;

namespace IdentityHub.Api.Models;

/// <summary>Represents a registered user with email, password hash, and navigation properties.</summary>
public class User
{
    public int Id { get; set; }

    [Required, MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    public JiraConfiguration? JiraConfiguration { get; set; }
    public ICollection<TicketReference> TicketReferences { get; set; } = [];
    public ICollection<ApiKey> ApiKeys { get; set; } = [];
}
