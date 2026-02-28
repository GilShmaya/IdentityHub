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
    [Required, MaxLength(32)] string ProjectKey,
    [MaxLength(128)] string? AssigneeAccountId = null,
    [MaxLength(32)] string? Priority = null
);

public record TicketResponse(string JiraIssueKey, string Title, string SelfUrl, DateTime CreatedAt);

public record TicketDetailResponse(
    string JiraIssueKey,
    string Title,
    string? Description,
    string Status,
    string? Priority,
    string? AssigneeAccountId,
    string? AssigneeDisplayName,
    string SelfUrl,
    IEnumerable<TicketCommentResponse> Comments
);

public record TicketCommentResponse(
    string Id,
    string AuthorDisplayName,
    string Body,
    DateTime Created
);

public record UpdateTicketRequest(
    [MaxLength(512)] string? Title = null,
    [MaxLength(8000)] string? Description = null,
    [MaxLength(128)] string? AssigneeAccountId = null,
    [MaxLength(32)] string? Priority = null
);

public record AddCommentRequest(
    [Required, MaxLength(8000)] string Body
);

public record JiraUserResponse(string AccountId, string DisplayName, string? AvatarUrl);

public record CreateFindingRequest(
    [Required, MaxLength(512)] string Title,
    [Required, MaxLength(8000)] string Description,
    [Required, MaxLength(32)] string ProjectKey,
    [MaxLength(128)] string? AssigneeAccountId = null,
    [MaxLength(32)] string? Priority = null
);

public record BatchTicketResult(
    string Title,
    bool Success,
    TicketResponse? Ticket,
    string? Error
);

public record ExternalCreateTicketsRequest(
    [Required, EmailAddress] string JiraEmail,
    [Required] string JiraApiToken,
    [Required, Url] string JiraSiteUrl,
    [Required] List<CreateFindingRequest> Tickets
);
