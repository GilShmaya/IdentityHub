using IdentityHub.Api.DTOs;
using IdentityHub.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityHub.Api.Controllers;

[Route("api/jira")]
[Authorize]
public class JiraController : BaseController
{
    private readonly IJiraConfigService _configService;
    private readonly IJiraService _jiraService;

    public JiraController(IJiraConfigService configService, IJiraService jiraService)
    {
        _configService = configService;
        _jiraService = jiraService;
    }

    /// <summary>
    /// Save or update the authenticated user's Jira connection credentials.
    /// </summary>
    /// <remarks>
    /// Route: POST /api/jira/config
    ///
    /// Requires JWT Bearer token. The API token is encrypted at rest.
    /// </remarks>
    /// <param name="request">Jira credentials: email, API token, and site URL.</param>
    /// <response code="200">Configuration saved successfully.</response>
    /// <response code="400">Invalid input (missing or malformed fields).</response>
    /// <response code="401">User is not authenticated.</response>
    [HttpPost("config")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SaveConfig([FromBody] SaveJiraConfigRequest request)
    {
        await _configService.SaveConfigAsync(GetUserId(), request);
        return Ok(new { message = "Jira configuration saved successfully." });
    }

    /// <summary>
    /// Get the authenticated user's Jira connection status and configuration.
    /// </summary>
    /// <remarks>
    /// Route: GET /api/jira/config
    ///
    /// Requires JWT Bearer token. Never returns the raw API token.
    /// </remarks>
    /// <returns>Jira email, site URL, and whether a configuration exists.</returns>
    /// <response code="200">Returns the current Jira configuration status.</response>
    /// <response code="401">User is not authenticated.</response>
    [HttpGet("config")]
    [ProducesResponseType(typeof(JiraConfigResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<JiraConfigResponse>> GetConfig()
    {
        var config = await _configService.GetConfigAsync(GetUserId());
        return Ok(config);
    }

    /// <summary>
    /// List available Jira projects from the user's connected Jira site.
    /// </summary>
    /// <remarks>
    /// Route: GET /api/jira/projects
    ///
    /// Requires JWT Bearer token and a saved Jira configuration.
    /// </remarks>
    /// <returns>A list of Jira projects (key and name).</returns>
    /// <response code="200">Returns available Jira projects.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="404">No Jira configuration found for this user.</response>
    [HttpGet("projects")]
    [ProducesResponseType(typeof(IEnumerable<JiraProjectResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<JiraProjectResponse>>> GetProjects()
    {
        var projects = await _jiraService.GetProjectsAsync(GetUserId());
        return Ok(projects);
    }

    /// <summary>
    /// Get users assignable to issues in a specific Jira project.
    /// </summary>
    /// <remarks>
    /// Route: GET /api/jira/projects/{projectKey}/users
    ///
    /// Requires JWT Bearer token and a saved Jira configuration.
    /// Returns up to 100 users who can be assigned to issues in the given project.
    /// </remarks>
    /// <param name="projectKey">The Jira project key (e.g., "NHI").</param>
    /// <returns>A list of assignable users (account ID, display name, avatar URL).</returns>
    /// <response code="200">Returns assignable users for the project.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="404">No Jira configuration found for this user.</response>
    [HttpGet("projects/{projectKey}/users")]
    [ProducesResponseType(typeof(IEnumerable<JiraUserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<JiraUserResponse>>> GetAssignableUsers(string projectKey)
    {
        var users = await _jiraService.GetAssignableUsersAsync(GetUserId(), projectKey);
        return Ok(users);
    }

    /// <summary>
    /// Create an NHI finding ticket in the specified Jira project.
    /// </summary>
    /// <remarks>
    /// Route: POST /api/jira/tickets
    ///
    /// Requires JWT Bearer token and a saved Jira configuration.
    /// The ticket is created as a Jira Task with the description in ADF format.
    /// </remarks>
    /// <param name="request">Ticket details: title, description, and target project key.</param>
    /// <returns>The created ticket reference including the Jira issue key.</returns>
    /// <response code="201">Ticket created successfully.</response>
    /// <response code="400">Invalid input (missing or malformed fields).</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="404">No Jira configuration found for this user.</response>
    [HttpPost("tickets")]
    [ProducesResponseType(typeof(TicketResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TicketResponse>> CreateTicket([FromBody] CreateTicketRequest request)
    {
        var ticket = await _jiraService.CreateTicketAsync(GetUserId(), request);
        return CreatedAtAction(nameof(GetRecentTickets), new { projectKey = request.ProjectKey }, ticket);
    }

    /// <summary>
    /// Get the 10 most recent tickets created in a Jira project through IdentityHub.
    /// </summary>
    /// <remarks>
    /// Route: GET /api/jira/tickets/recent?projectKey={key}
    ///
    /// Requires JWT Bearer token. Returns tickets created by any user in the organization
    /// for the specified project, ordered by most recent first.
    /// </remarks>
    /// <param name="projectKey">The Jira project key to filter tickets by (e.g., "NHI").</param>
    /// <returns>Up to 10 most recent ticket references.</returns>
    /// <response code="200">Returns recent tickets.</response>
    /// <response code="400">The projectKey query parameter is missing.</response>
    /// <response code="401">User is not authenticated.</response>
    [HttpGet("tickets/recent")]
    [ProducesResponseType(typeof(IEnumerable<TicketResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<TicketResponse>>> GetRecentTickets([FromQuery] string projectKey)
    {
        if (string.IsNullOrWhiteSpace(projectKey))
            return BadRequest(new { error = "projectKey query parameter is required." });

        var tickets = await _jiraService.GetRecentTicketsAsync(GetUserId(), projectKey);
        return Ok(tickets);
    }

    /// <summary>
    /// Get full details of a specific Jira ticket including status, assignee, and comments.
    /// </summary>
    /// <remarks>
    /// Route: GET /api/jira/tickets/{issueKey}
    ///
    /// Requires JWT Bearer token. Fetches live data from Jira.
    /// </remarks>
    /// <param name="issueKey">The Jira issue key (e.g., "NHI-42").</param>
    /// <returns>Full ticket details with comments.</returns>
    /// <response code="200">Returns ticket details.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="404">Ticket not found in Jira or no Jira configuration.</response>
    [HttpGet("tickets/{issueKey}")]
    [ProducesResponseType(typeof(TicketDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TicketDetailResponse>> GetTicket(string issueKey)
    {
        var ticket = await _jiraService.GetTicketAsync(GetUserId(), issueKey);
        return Ok(ticket);
    }

    /// <summary>
    /// Update fields of an existing Jira ticket (title, description, assignee, priority).
    /// </summary>
    /// <remarks>
    /// Route: PUT /api/jira/tickets/{issueKey}
    ///
    /// Requires JWT Bearer token. Only provided fields are updated; omitted fields remain unchanged.
    /// Send empty string for assigneeAccountId to unassign.
    /// </remarks>
    /// <param name="issueKey">The Jira issue key (e.g., "NHI-42").</param>
    /// <param name="request">Fields to update (all optional).</param>
    /// <response code="204">Ticket updated successfully.</response>
    /// <response code="400">Invalid input.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="404">Ticket not found in Jira.</response>
    [HttpPut("tickets/{issueKey}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTicket(string issueKey, [FromBody] UpdateTicketRequest request)
    {
        await _jiraService.UpdateTicketAsync(GetUserId(), issueKey, request);
        return NoContent();
    }

    /// <summary>
    /// Add a comment to an existing Jira ticket.
    /// </summary>
    /// <remarks>
    /// Route: POST /api/jira/tickets/{issueKey}/comments
    ///
    /// Requires JWT Bearer token. The comment is posted using the Jira credentials of the user.
    /// </remarks>
    /// <param name="issueKey">The Jira issue key (e.g., "NHI-42").</param>
    /// <param name="request">The comment body text.</param>
    /// <returns>The created comment.</returns>
    /// <response code="201">Comment added successfully.</response>
    /// <response code="400">Empty comment body.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="404">Ticket not found in Jira.</response>
    [HttpPost("tickets/{issueKey}/comments")]
    [ProducesResponseType(typeof(TicketCommentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TicketCommentResponse>> AddComment(string issueKey, [FromBody] AddCommentRequest request)
    {
        var comment = await _jiraService.AddCommentAsync(GetUserId(), issueKey, request);
        return StatusCode(201, comment);
    }

}
