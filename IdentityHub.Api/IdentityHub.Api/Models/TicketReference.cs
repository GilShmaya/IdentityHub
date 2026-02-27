using System.ComponentModel.DataAnnotations;

namespace IdentityHub.Api.Models;

public class TicketReference
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    [Required, MaxLength(64)]
    public string JiraIssueKey { get; set; } = string.Empty;

    [Required, MaxLength(32)]
    public string ProjectKey { get; set; } = string.Empty;

    [Required, MaxLength(512)]
    public string Title { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
