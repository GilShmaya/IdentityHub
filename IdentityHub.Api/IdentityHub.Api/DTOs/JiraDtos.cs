using System.ComponentModel.DataAnnotations;

namespace IdentityHub.Api.DTOs;

public record SaveJiraConfigRequest(
    [Required, EmailAddress, MaxLength(256)] string Email,
    [Required] string ApiToken,
    [Required, Url, MaxLength(512)] string SiteUrl
);

public record JiraConfigResponse(string Email, string SiteUrl, bool IsConfigured);

public record JiraProjectResponse(string Key, string Name);

public record CreateTicketRequest(
    [Required, MaxLength(512)] string Title,
    [Required, MaxLength(8000)] string Description,
    [Required, MaxLength(32)] string ProjectKey
);

public record TicketResponse(string JiraIssueKey, string Title, string SelfUrl, DateTime CreatedAt);

public record CreateFindingRequest(
    [Required, MaxLength(512)] string Title,
    [Required, MaxLength(8000)] string Description,
    [Required, MaxLength(32)] string ProjectKey
);
