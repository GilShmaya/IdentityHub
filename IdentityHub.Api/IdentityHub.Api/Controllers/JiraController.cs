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

    [HttpPost("config")]
    public async Task<IActionResult> SaveConfig([FromBody] SaveJiraConfigRequest request)
    {
        await _configService.SaveConfigAsync(GetUserId(), request);
        return Ok(new { message = "Jira configuration saved successfully." });
    }

    [HttpGet("config")]
    public async Task<ActionResult<JiraConfigResponse>> GetConfig()
    {
        var config = await _configService.GetConfigAsync(GetUserId());
        return Ok(config);
    }

    [HttpGet("projects")]
    public async Task<ActionResult<IEnumerable<JiraProjectResponse>>> GetProjects()
    {
        var projects = await _jiraService.GetProjectsAsync(GetUserId());
        return Ok(projects);
    }

    [HttpPost("tickets")]
    public async Task<ActionResult<TicketResponse>> CreateTicket([FromBody] CreateTicketRequest request)
    {
        var ticket = await _jiraService.CreateTicketAsync(GetUserId(), request);
        return CreatedAtAction(nameof(GetRecentTickets), new { projectKey = request.ProjectKey }, ticket);
    }

    [HttpGet("tickets/recent")]
    public async Task<ActionResult<IEnumerable<TicketResponse>>> GetRecentTickets([FromQuery] string projectKey)
    {
        if (string.IsNullOrWhiteSpace(projectKey))
            return BadRequest(new { error = "projectKey query parameter is required." });

        var tickets = await _jiraService.GetRecentTicketsAsync(GetUserId(), projectKey);
        return Ok(tickets);
    }
}
