using IdentityHub.Api.DTOs;

namespace IdentityHub.Api.Services;

public interface IJiraConfigService
{
    Task SaveConfigAsync(int userId, SaveJiraConfigRequest request);
    Task<JiraConfigResponse> GetConfigAsync(int userId);
    Task<(string Email, string ApiToken, string SiteUrl)> GetCredentialsAsync(int userId);
}
