using IdentityHub.Api.DTOs;

namespace IdentityHub.Api.Services;

/// <summary>Contract for managing per-user Jira credential storage and retrieval.</summary>
public interface IJiraConfigService
{
    Task SaveConfigAsync(int userId, SaveJiraConfigRequest request);
    Task<JiraConfigResponse> GetConfigAsync(int userId);
    Task<(string Email, string ApiToken, string SiteUrl)> GetCredentialsAsync(int userId);
}
