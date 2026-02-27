using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using IdentityHub.Api.Data;
using IdentityHub.Api.DTOs;
using IdentityHub.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace IdentityHub.Api.Services;

public class JiraService : IJiraService
{
    private readonly IJiraConfigService _configService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AppDbContext _db;
    private readonly ILogger<JiraService> _logger;

    public JiraService(
        IJiraConfigService configService,
        IHttpClientFactory httpClientFactory,
        AppDbContext db,
        ILogger<JiraService> logger)
    {
        _configService = configService;
        _httpClientFactory = httpClientFactory;
        _db = db;
        _logger = logger;
    }

    public async Task<IEnumerable<JiraProjectResponse>> GetProjectsAsync(int userId)
    {
        var client = await CreateClientAsync(userId);
        var response = await client.GetAsync("rest/api/3/project");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.EnumerateArray().Select(p => new JiraProjectResponse(
            p.GetProperty("key").GetString()!,
            p.GetProperty("name").GetString()!
        ));
    }

    public async Task<TicketResponse> CreateTicketAsync(int userId, CreateTicketRequest request)
    {
        var (_, _, siteUrl) = await _configService.GetCredentialsAsync(userId);
        var client = await CreateClientAsync(userId);

        var payload = new
        {
            fields = new
            {
                project = new { key = request.ProjectKey },
                summary = request.Title,
                description = new
                {
                    type = "doc",
                    version = 1,
                    content = new[]
                    {
                        new
                        {
                            type = "paragraph",
                            content = new[]
                            {
                                new { type = "text", text = request.Description }
                            }
                        }
                    }
                },
                issuetype = new { name = "Task" }
            }
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await client.PostAsync("rest/api/3/issue", content);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            _logger.LogError("Jira API error: {StatusCode} {Error}", response.StatusCode, error);
            throw new HttpRequestException($"Failed to create Jira ticket: {response.StatusCode}");
        }

        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        var issueKey = result.GetProperty("key").GetString()!;
        var selfUrl = $"{siteUrl}/browse/{issueKey}";

        var ticketRef = new TicketReference
        {
            UserId = userId,
            JiraIssueKey = issueKey,
            ProjectKey = request.ProjectKey,
            Title = request.Title,
            CreatedAt = DateTime.UtcNow
        };
        _db.TicketReferences.Add(ticketRef);
        await _db.SaveChangesAsync();

        return new TicketResponse(issueKey, request.Title, selfUrl, ticketRef.CreatedAt);
    }

    public async Task<IEnumerable<TicketResponse>> GetRecentTicketsAsync(int userId, string projectKey)
    {
        var (_, _, siteUrl) = await _configService.GetCredentialsAsync(userId);

        var tickets = await _db.TicketReferences
            .Where(t => t.UserId == userId && t.ProjectKey == projectKey)
            .OrderByDescending(t => t.CreatedAt)
            .Take(10)
            .ToListAsync();

        return tickets.Select(t => new TicketResponse(
            t.JiraIssueKey,
            t.Title,
            $"{siteUrl}/browse/{t.JiraIssueKey}",
            t.CreatedAt
        ));
    }

    private async Task<HttpClient> CreateClientAsync(int userId)
    {
        var (email, apiToken, siteUrl) = await _configService.GetCredentialsAsync(userId);
        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(siteUrl.TrimEnd('/') + "/");

        var authBytes = Encoding.UTF8.GetBytes($"{email}:{apiToken}");
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        return client;
    }
}
