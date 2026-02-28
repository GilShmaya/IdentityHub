using IdentityHub.Api.DTOs;
using IdentityHub.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace IdentityHub.Api.Controllers;

[Route("api/v1/tickets")]
[ApiController]
public class FindingsController : ControllerBase
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
    /// Stateless endpoint — no user account required. The caller provides their Jira credentials
    /// (email, API token, site URL) directly in the request body along with the tickets to create.
    /// Uses Jira's bulk create API for efficient processing.
    /// If some tickets fail, the response includes both successes and errors (HTTP 207 Multi-Status).
    /// </remarks>
    /// <param name="request">Jira credentials and an array of ticket objects (1–50).</param>
    /// <returns>An array of results, each indicating success (with ticket data) or failure (with error message).</returns>
    /// <response code="201">All tickets created successfully.</response>
    /// <response code="207">Partial success — some tickets failed. Check individual results.</response>
    /// <response code="400">Missing credentials, empty tickets array, or batch exceeds 50.</response>
    /// <response code="401">Invalid Jira credentials.</response>
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

        var ticketRequests = request.Tickets
            .Select(r => new CreateTicketRequest(r.Title, r.Description, r.ProjectKey, r.AssigneeAccountId, r.Priority))
            .ToList();

        var results = await _jiraService.CreateTicketsBulkWithCredentialsAsync(
            request.JiraEmail, request.JiraApiToken, request.JiraSiteUrl, ticketRequests);
        var resultList = results.ToList();

        var hasFailures = resultList.Any(r => !r.Success);
        return StatusCode(hasFailures ? 207 : 201, resultList);
    }

    /// <summary>
    /// Get the 10 most recent tickets created in a Jira project through IdentityHub.
    /// </summary>
    /// <remarks>
    /// Route: GET /api/v1/tickets?projectKey={key}
    ///
    /// Requires Jira credentials via headers: X-Jira-Email, X-Jira-Api-Token, X-Jira-Site-Url.
    /// Credentials are validated against the Jira API before returning data.
    /// Returns the 10 most recent tickets created by any user in the organization for the specified project.
    /// </remarks>
    /// <param name="projectKey">The Jira project key (e.g., "NHI").</param>
    /// <returns>Up to 10 most recent ticket references.</returns>
    /// <response code="200">Returns recent tickets.</response>
    /// <response code="400">Missing projectKey or Jira credential headers.</response>
    /// <response code="401">Invalid Jira credentials.</response>
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

        var tickets = await _jiraService.GetRecentTicketsAsync(siteUrl, projectKey);
        return Ok(tickets);
    }
}
