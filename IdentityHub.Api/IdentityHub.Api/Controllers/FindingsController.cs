using IdentityHub.Api.DTOs;
using IdentityHub.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityHub.Api.Controllers;

[Route("api/v1/findings")]
[Authorize(AuthenticationSchemes = "ApiKey")]
public class FindingsController : BaseController
{
    private readonly IJiraService _jiraService;

    public FindingsController(IJiraService jiraService)
    {
        _jiraService = jiraService;
    }

    [HttpPost]
    public async Task<ActionResult<TicketResponse>> CreateFinding([FromBody] CreateFindingRequest request)
    {
        var ticket = await _jiraService.CreateTicketAsync(
            GetUserId(),
            new CreateTicketRequest(request.Title, request.Description, request.ProjectKey));

        return CreatedAtAction(null, ticket);
    }
}
