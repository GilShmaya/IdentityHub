using System.Security.Claims;
using IdentityHub.Api.Controllers.PublicApi;
using IdentityHub.Api.DTOs;
using IdentityHub.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace IdentityHub.Tests;

public class FindingsControllerTests
{
    private const int TEST_USER_ID = 1;

    private static FindingsController CreateController(Mock<IJiraService> mockJira)
    {
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, TEST_USER_ID.ToString()) };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        var controller = new FindingsController(mockJira.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            }
        };
        return controller;
    }

    private static ExternalCreateTicketsRequest MakeRequest(int ticketCount = 1) =>
        new(
            "jira@test.com", "token", "https://test.atlassian.net",
            Enumerable.Range(1, ticketCount)
                .Select(i => new CreateFindingRequest($"Title {i}", $"Desc {i}", "NHI", null, null))
                .ToList()
        );

    // --- CreateTickets ---

    [Fact]
    public async Task CreateTickets_EmptyList_ReturnsBadRequest()
    {
        var mock = new Mock<IJiraService>();
        var controller = CreateController(mock);
        var request = new ExternalCreateTicketsRequest("a@b.com", "tok", "https://site.atlassian.net", []);

        var result = await controller.CreateTickets(request) as ObjectResult;

        Assert.Equal(400, result!.StatusCode);
    }

    [Fact]
    public async Task CreateTickets_ExceedsBatchSize_ReturnsBadRequest()
    {
        var mock = new Mock<IJiraService>();
        var controller = CreateController(mock);
        var request = MakeRequest(51);

        var result = await controller.CreateTickets(request) as ObjectResult;

        Assert.Equal(400, result!.StatusCode);
    }

    [Fact]
    public async Task CreateTickets_AllSuccess_Returns201()
    {
        var mock = new Mock<IJiraService>();
        mock.Setup(s => s.CreateTicketsBulkWithCredentialsAsync(
                TEST_USER_ID, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IList<CreateTicketRequest>>()))
            .ReturnsAsync([new BatchTicketResult("Title 1", true, new TicketResponse("NHI-1", "Title 1", "https://site/browse/NHI-1", DateTime.UtcNow), null)]);

        var controller = CreateController(mock);
        var result = await controller.CreateTickets(MakeRequest()) as ObjectResult;

        Assert.Equal(201, result!.StatusCode);
    }

    [Fact]
    public async Task CreateTickets_PartialFailure_Returns207()
    {
        var mock = new Mock<IJiraService>();
        mock.Setup(s => s.CreateTicketsBulkWithCredentialsAsync(
                TEST_USER_ID, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IList<CreateTicketRequest>>()))
            .ReturnsAsync([
                new BatchTicketResult("Title 1", true, new TicketResponse("NHI-1", "Title 1", "https://site/browse/NHI-1", DateTime.UtcNow), null),
                new BatchTicketResult("Title 2", false, null, "Jira error")
            ]);

        var controller = CreateController(mock);
        var request = MakeRequest(2);
        var result = await controller.CreateTickets(request) as ObjectResult;

        Assert.Equal(207, result!.StatusCode);
    }

    [Fact]
    public async Task CreateTickets_MaxBatchSize_Accepted()
    {
        var mock = new Mock<IJiraService>();
        mock.Setup(s => s.CreateTicketsBulkWithCredentialsAsync(
                TEST_USER_ID, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IList<CreateTicketRequest>>()))
            .ReturnsAsync(Enumerable.Range(1, 50)
                .Select(i => new BatchTicketResult($"Title {i}", true, new TicketResponse($"NHI-{i}", $"Title {i}", $"https://site/browse/NHI-{i}", DateTime.UtcNow), null))
                .ToList());

        var controller = CreateController(mock);
        var result = await controller.CreateTickets(MakeRequest(50)) as ObjectResult;

        Assert.Equal(201, result!.StatusCode);
    }

    // --- GetRecentTickets ---

    [Fact]
    public async Task GetRecentTickets_MissingProjectKey_ReturnsBadRequest()
    {
        var mock = new Mock<IJiraService>();
        var controller = CreateController(mock);

        var result = await controller.GetRecentTickets("") as ObjectResult;

        Assert.Equal(400, result!.StatusCode);
    }

    [Fact]
    public async Task GetRecentTickets_MissingHeaders_ReturnsBadRequest()
    {
        var mock = new Mock<IJiraService>();
        var controller = CreateController(mock);

        var result = await controller.GetRecentTickets("NHI") as ObjectResult;

        Assert.Equal(400, result!.StatusCode);
    }

    [Fact]
    public async Task GetRecentTickets_InvalidCredentials_ReturnsUnauthorized()
    {
        var mock = new Mock<IJiraService>();
        mock.Setup(s => s.ValidateCredentialsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(false);

        var controller = CreateController(mock);
        controller.HttpContext.Request.Headers["X-Jira-Email"] = "a@b.com";
        controller.HttpContext.Request.Headers["X-Jira-Api-Token"] = "bad-token";
        controller.HttpContext.Request.Headers["X-Jira-Site-Url"] = "https://site.atlassian.net";

        var result = await controller.GetRecentTickets("NHI") as ObjectResult;

        Assert.Equal(401, result!.StatusCode);
    }

    [Fact]
    public async Task GetRecentTickets_ValidCredentials_ReturnsOk()
    {
        var mock = new Mock<IJiraService>();
        mock.Setup(s => s.ValidateCredentialsAsync("a@b.com", "good-token", "https://site.atlassian.net"))
            .ReturnsAsync(true);
        mock.Setup(s => s.GetRecentTicketsAsync(TEST_USER_ID, "NHI"))
            .ReturnsAsync([new TicketResponse("NHI-1", "Title", "https://site/browse/NHI-1", DateTime.UtcNow)]);

        var controller = CreateController(mock);
        controller.HttpContext.Request.Headers["X-Jira-Email"] = "a@b.com";
        controller.HttpContext.Request.Headers["X-Jira-Api-Token"] = "good-token";
        controller.HttpContext.Request.Headers["X-Jira-Site-Url"] = "https://site.atlassian.net";

        var result = await controller.GetRecentTickets("NHI") as OkObjectResult;

        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);
    }

    [Fact]
    public async Task GetRecentTickets_PartialHeaders_ReturnsBadRequest()
    {
        var mock = new Mock<IJiraService>();
        var controller = CreateController(mock);
        controller.HttpContext.Request.Headers["X-Jira-Email"] = "a@b.com";

        var result = await controller.GetRecentTickets("NHI") as ObjectResult;

        Assert.Equal(400, result!.StatusCode);
    }
}
