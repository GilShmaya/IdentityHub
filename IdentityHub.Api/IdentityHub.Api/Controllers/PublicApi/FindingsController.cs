using IdentityHub.Api.Authentication;
using IdentityHub.Api.DTOs;
using IdentityHub.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityHub.Api.Controllers.PublicApi;

/// <summary>External REST API endpoint for programmatic NHI finding ticket creation.</summary>
[Route("api/v1/tickets")]
[ApiController]
[Authorize(AuthenticationSchemes = ApiKeyAuthDefaults.AuthenticationScheme)]
public class FindingsController : BaseController
{
    private const int MAX_BATCH_SIZE = 50;
    private readonly IJiraService _jiraService;

    public FindingsController(IJiraService jiraService)
    {
        _jiraService = jiraService;
    }

    /// <summary>
    /// Create one or more NHI finding tickets via the external API (batch supported).
    /// </summary>
    /// <remarks>
    /// Route: POST /api/v1/tickets
    ///
    /// Requires API key authentication via the X-Api-Key header.
    /// The caller provides their Jira credentials (email, API token, site URL) in the request body
    /// along with the tickets to create. Uses Jira's bulk create API for efficient processing.
    /// Tickets are saved to the user's account and visible in the web portal.
    /// If some tickets fail, the response includes both successes and errors (HTTP 207 Multi-Status).
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(IEnumerable<BatchTicketResult>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(IEnumerable<BatchTicketResult>), 207)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateTickets([FromBody] ExternalCreateTicketsRequest request)
    {
        if (request.Tickets.Count == 0)
            return BadRequest(new { error = "At least one ticket is required." });

        if (request.Tickets.Count > MAX_BATCH_SIZE)
            return BadRequest(new { error = $"Batch size exceeds the maximum of {MAX_BATCH_SIZE} tickets per request." });
        
        if (!await _jiraService.ValidateCredentialsAsync(request.JiraEmail, request.JiraApiToken, request.JiraSiteUrl))
            return Unauthorized(new { error = "Invalid Jira credentials." });

        var userId = GetUserId();
        var ticketRequests = request.Tickets
            .Select(r => new CreateTicketRequest(r.Title, r.Description, r.ProjectKey, r.AssigneeAccountId, r.Priority))
            .ToList();

        var results = await _jiraService.CreateTicketsBulkWithCredentialsAsync(
            userId, request.JiraEmail, request.JiraApiToken, request.JiraSiteUrl, ticketRequests);
        var resultList = results.ToList();

        var hasFailures = resultList.Any(r => !r.Success);
        return StatusCode(hasFailures ? 207 : 201, resultList);
    }

    /// <summary>
    /// Get the 10 most recent tickets created by the authenticated user in a Jira project.
    /// </summary>
    /// <remarks>
    /// Route: GET /api/v1/tickets?projectKey={key}
    ///
    /// Requires API key authentication via the X-Api-Key header.
    /// Also requires Jira credentials via headers: X-Jira-Email, X-Jira-Api-Token, X-Jira-Site-Url.
    /// Returns the 10 most recent tickets created by this user for the specified project.
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<TicketResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetRecentTickets([FromQuery] string projectKey)
    {
        if (string.IsNullOrWhiteSpace(projectKey))
            return BadRequest(new { error = "projectKey query parameter is required." });

        var email = Request.Headers["X-Jira-Email"].ToString();
        var apiToken = Request.Headers["X-Jira-Api-Token"].ToString();
        var siteUrl = Request.Headers["X-Jira-Site-Url"].ToString();

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(apiToken) || string.IsNullOrWhiteSpace(siteUrl))
            return BadRequest(new { error = "Missing required headers: X-Jira-Email, X-Jira-Api-Token, X-Jira-Site-Url." });

        if (!await _jiraService.ValidateCredentialsAsync(email, apiToken, siteUrl))
            return Unauthorized(new { error = "Invalid Jira credentials." });

        var tickets = await _jiraService.GetRecentTicketsAsync(GetUserId(), projectKey);
        return Ok(tickets);
    }
}
