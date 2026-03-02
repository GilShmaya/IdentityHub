using System.ComponentModel.DataAnnotations;

namespace IdentityHub.Api.DTOs;

/// <summary>Request payload for saving or updating Jira credentials.</summary>
public record SaveJiraConfigRequest(
    [Required, EmailAddress, MaxLength(256)] string Email,
    [Required] string ApiToken,
    [Required, Url, MaxLength(512)] string SiteUrl
);

/// <summary>Response indicating the current Jira connection status for a user.</summary>
public record JiraConfigResponse(string Email, string SiteUrl, bool IsConfigured);

/// <summary>Represents a Jira project with its key and display name.</summary>
public record JiraProjectResponse(string Key, string Name);

/// <summary>Request payload for creating a Jira ticket.</summary>
public record CreateTicketRequest(
    [Required, MaxLength(512)] string Title,
    [Required, MaxLength(8000)] string Description,
    [Required, MaxLength(32)] string ProjectKey,
    [MaxLength(128)] string? AssigneeAccountId = null,
    [MaxLength(32)] string? Priority = null
);

/// <summary>Summary of a created or retrieved Jira ticket.</summary>
public record TicketResponse(string JiraIssueKey, string Title, string SelfUrl, DateTime CreatedAt);

/// <summary>Detailed view of a Jira ticket including status, priority, and comments.</summary>
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

/// <summary>Represents a comment on a Jira ticket.</summary>
public record TicketCommentResponse(
    string Id,
    string AuthorDisplayName,
    string Body,
    DateTime Created
);

/// <summary>Request payload for updating an existing Jira ticket.</summary>
public record UpdateTicketRequest(
    [MaxLength(512)] string? Title = null,
    [MaxLength(8000)] string? Description = null,
    [MaxLength(128)] string? AssigneeAccountId = null,
    [MaxLength(32)] string? Priority = null
);

/// <summary>Represents an available workflow transition for a Jira ticket.</summary>
public record TransitionResponse(string Id, string Name);

/// <summary>Request payload for transitioning a Jira ticket to a new status.</summary>
public record TransitionRequest(
    [Required] string TransitionId
);

/// <summary>Request payload for adding a comment to a Jira ticket.</summary>
public record AddCommentRequest(
    [Required, MaxLength(8000)] string Body
);

/// <summary>Represents a Jira user assignable to a project.</summary>
public record JiraUserResponse(string AccountId, string DisplayName, string? AvatarUrl);

/// <summary>Request payload for creating an NHI finding via the external API.</summary>
public record CreateFindingRequest(
    [Required, MaxLength(512)] string Title,
    [Required, MaxLength(8000)] string Description,
    [Required, MaxLength(32)] string ProjectKey,
    [MaxLength(128)] string? AssigneeAccountId = null,
    [MaxLength(32)] string? Priority = null
);

/// <summary>Result of a single ticket in a bulk creation operation.</summary>
public record BatchTicketResult(
    string Title,
    bool Success,
    TicketResponse? Ticket,
    string? Error
);

/// <summary>External API request containing Jira credentials and tickets to create.</summary>
public record ExternalCreateTicketsRequest(
    [Required, EmailAddress] string JiraEmail,
    [Required] string JiraApiToken,
    [Required, Url] string JiraSiteUrl,
    [Required] List<CreateFindingRequest> Tickets
);
