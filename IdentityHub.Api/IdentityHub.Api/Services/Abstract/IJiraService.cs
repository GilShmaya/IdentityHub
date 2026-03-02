using IdentityHub.Api.DTOs;

namespace IdentityHub.Api.Services;

/// <summary>Contract for Jira ticket CRUD and bulk operations.</summary>
public interface IJiraService
{
    Task<IEnumerable<JiraProjectResponse>> GetProjectsAsync(int userId);
    Task<IEnumerable<JiraUserResponse>> GetAssignableUsersAsync(int userId, string projectKey);
    Task<TicketResponse> CreateTicketAsync(int userId, CreateTicketRequest request);
    Task<TicketDetailResponse> GetTicketAsync(int userId, string issueKey);
    Task UpdateTicketAsync(int userId, string issueKey, UpdateTicketRequest request);
    Task<TicketCommentResponse> AddCommentAsync(int userId, string issueKey, AddCommentRequest request);
    Task<IEnumerable<TransitionResponse>> GetTransitionsAsync(int userId, string issueKey);
    Task TransitionTicketAsync(int userId, string issueKey, TransitionRequest request);
    Task<IEnumerable<BatchTicketResult>> CreateTicketsBulkAsync(int userId, IList<CreateTicketRequest> requests);
    Task<IEnumerable<BatchTicketResult>> CreateTicketsBulkWithCredentialsAsync(int userId, string email, string apiToken, string siteUrl, IList<CreateTicketRequest> requests);
    Task<IEnumerable<TicketResponse>> GetRecentTicketsAsync(int userId, string projectKey);
    Task<bool> ValidateCredentialsAsync(string email, string apiToken, string siteUrl);
}
