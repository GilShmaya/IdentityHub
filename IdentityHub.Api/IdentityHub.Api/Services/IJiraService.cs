using IdentityHub.Api.DTOs;

namespace IdentityHub.Api.Services;

public interface IJiraService
{
    Task<IEnumerable<JiraProjectResponse>> GetProjectsAsync(int userId);
    Task<TicketResponse> CreateTicketAsync(int userId, CreateTicketRequest request);
    Task<IEnumerable<TicketResponse>> GetRecentTicketsAsync(int userId, string projectKey);
}
