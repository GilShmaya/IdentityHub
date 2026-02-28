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

    public async Task<IEnumerable<JiraUserResponse>> GetAssignableUsersAsync(int userId, string projectKey)
    {
        var client = await CreateClientAsync(userId);
        var response = await client.GetAsync($"rest/api/3/user/assignable/search?project={projectKey}&maxResults=100");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.EnumerateArray().Select(u => new JiraUserResponse(
            u.GetProperty("accountId").GetString()!,
            u.GetProperty("displayName").GetString()!,
            u.TryGetProperty("avatarUrls", out var avatars) && avatars.TryGetProperty("24x24", out var url)
                ? url.GetString()
                : null
        ));
    }

    public async Task<TicketResponse> CreateTicketAsync(int userId, CreateTicketRequest request)
    {
        var (_, _, siteUrl) = await _configService.GetCredentialsAsync(userId);
        var client = await CreateClientAsync(userId);

        var payload = new { fields = BuildFieldsDictionary(request) };

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

    public async Task<TicketDetailResponse> GetTicketAsync(int userId, string issueKey)
    {
        var (_, _, siteUrl) = await _configService.GetCredentialsAsync(userId);
        var client = await CreateClientAsync(userId);

        var response = await client.GetAsync($"rest/api/3/issue/{issueKey}?fields=summary,description,status,priority,assignee,comment");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var fields = json.GetProperty("fields");

        var description = ExtractPlainText(fields);
        var status = fields.GetProperty("status").GetProperty("name").GetString()!;
        var priority = fields.TryGetProperty("priority", out var p) && p.ValueKind != JsonValueKind.Null
            ? p.GetProperty("name").GetString() : null;
        var assigneeId = fields.TryGetProperty("assignee", out var a) && a.ValueKind != JsonValueKind.Null
            ? a.GetProperty("accountId").GetString() : null;
        var assigneeName = a.ValueKind != JsonValueKind.Null
            ? a.GetProperty("displayName").GetString() : null;

        var comments = new List<TicketCommentResponse>();
        if (fields.TryGetProperty("comment", out var commentBlock) &&
            commentBlock.TryGetProperty("comments", out var commentArray))
        {
            foreach (var c in commentArray.EnumerateArray())
            {
                comments.Add(new TicketCommentResponse(
                    c.GetProperty("id").GetString()!,
                    c.GetProperty("author").GetProperty("displayName").GetString()!,
                    ExtractCommentText(c.GetProperty("body")),
                    c.GetProperty("created").GetDateTime()
                ));
            }
        }

        return new TicketDetailResponse(
            issueKey,
            fields.GetProperty("summary").GetString()!,
            description,
            status,
            priority,
            assigneeId,
            assigneeName,
            $"{siteUrl}/browse/{issueKey}",
            comments
        );
    }

    public async Task UpdateTicketAsync(int userId, string issueKey, UpdateTicketRequest request)
    {
        var client = await CreateClientAsync(userId);
        var fields = new Dictionary<string, object>();

        if (request.Title is not null)
            fields["summary"] = request.Title;

        if (request.Description is not null)
        {
            fields["description"] = new
            {
                type = "doc",
                version = 1,
                content = new[]
                {
                    new
                    {
                        type = "paragraph",
                        content = new[] { new { type = "text", text = request.Description } }
                    }
                }
            };
        }

        if (request.AssigneeAccountId is not null)
            fields["assignee"] = request.AssigneeAccountId == "" ? null! : new { accountId = request.AssigneeAccountId };

        if (request.Priority is not null)
            fields["priority"] = new { name = request.Priority };

        var payload = new { fields };
        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await client.PutAsync($"rest/api/3/issue/{issueKey}", content);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            _logger.LogError("Jira update error: {StatusCode} {Error}", response.StatusCode, error);
            throw new HttpRequestException($"Failed to update ticket: {response.StatusCode}");
        }

        // Sync title change to local DB
        if (request.Title is not null)
        {
            var ticketRef = await _db.TicketReferences.FirstOrDefaultAsync(t => t.JiraIssueKey == issueKey);
            if (ticketRef is not null)
            {
                ticketRef.Title = request.Title;
                await _db.SaveChangesAsync();
            }
        }
    }

    public async Task<TicketCommentResponse> AddCommentAsync(int userId, string issueKey, AddCommentRequest request)
    {
        var client = await CreateClientAsync(userId);

        var payload = new
        {
            body = new
            {
                type = "doc",
                version = 1,
                content = new[]
                {
                    new
                    {
                        type = "paragraph",
                        content = new[] { new { type = "text", text = request.Body } }
                    }
                }
            }
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await client.PostAsync($"rest/api/3/issue/{issueKey}/comment", content);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            _logger.LogError("Jira comment error: {StatusCode} {Error}", response.StatusCode, error);
            throw new HttpRequestException($"Failed to add comment: {response.StatusCode}");
        }

        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        return new TicketCommentResponse(
            result.GetProperty("id").GetString()!,
            result.GetProperty("author").GetProperty("displayName").GetString()!,
            request.Body,
            result.GetProperty("created").GetDateTime()
        );
    }

    public async Task<IEnumerable<BatchTicketResult>> CreateTicketsBulkAsync(int userId, IList<CreateTicketRequest> requests)
    {
        var (_, _, siteUrl) = await _configService.GetCredentialsAsync(userId);
        var client = await CreateClientAsync(userId);

        var issueUpdates = requests.Select(r => new { fields = BuildFieldsDictionary(r) }).ToList();

        var payload = new { issueUpdates };
        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await client.PostAsync("rest/api/3/issue/bulk", content);

        var resultJson = await response.Content.ReadFromJsonAsync<JsonElement>();
        var results = new List<BatchTicketResult>();

        // Parse created issues
        if (resultJson.TryGetProperty("issues", out var issues))
        {
            var createdIssues = issues.EnumerateArray().ToList();
            var ticketRefs = new List<TicketReference>();

            for (var i = 0; i < createdIssues.Count; i++)
            {
                var issue = createdIssues[i];
                var issueKey = issue.GetProperty("key").GetString()!;
                var selfUrl = $"{siteUrl}/browse/{issueKey}";
                var request = requests[i];
                var createdAt = DateTime.UtcNow;

                ticketRefs.Add(new TicketReference
                {
                    UserId = userId,
                    JiraIssueKey = issueKey,
                    ProjectKey = request.ProjectKey,
                    Title = request.Title,
                    CreatedAt = createdAt
                });

                results.Add(new BatchTicketResult(
                    request.Title, true,
                    new TicketResponse(issueKey, request.Title, selfUrl, createdAt),
                    null));
            }

            if (ticketRefs.Count > 0)
            {
                _db.TicketReferences.AddRange(ticketRefs);
                await _db.SaveChangesAsync();
            }
        }

        // Parse errors — Jira returns errors for issues that failed
        if (resultJson.TryGetProperty("errors", out var errors))
        {
            foreach (var error in errors.EnumerateArray())
            {
                var errorMessages = error.TryGetProperty("elementErrors", out var elementErrors)
                    && elementErrors.TryGetProperty("errors", out var errMap)
                        ? string.Join("; ", errMap.EnumerateObject().Select(e => $"{e.Name}: {e.Value}"))
                        : "Unknown Jira error";

                var failedIndex = error.TryGetProperty("failedElementNumber", out var num)
                    ? num.GetInt32()
                    : -1;

                var title = failedIndex >= 0 && failedIndex < requests.Count
                    ? requests[failedIndex].Title
                    : "Unknown";

                results.Add(new BatchTicketResult(title, false, null, errorMessages));
            }
        }

        return results;
    }

    public async Task<IEnumerable<TicketResponse>> GetRecentTicketsAsync(int userId, string projectKey)
    {
        var (_, _, siteUrl) = await _configService.GetCredentialsAsync(userId);
        return await GetRecentTicketsAsync(siteUrl, projectKey);
    }

    public async Task<IEnumerable<TicketResponse>> GetRecentTicketsAsync(string siteUrl, string projectKey)
    {
        var tickets = await _db.TicketReferences
            .Where(t => t.ProjectKey == projectKey)
            .OrderByDescending(t => t.CreatedAt)
            .Take(10)
            .ToListAsync();

        return tickets.Select(t => new TicketResponse(
            t.JiraIssueKey,
            t.Title,
            $"{siteUrl.TrimEnd('/')}/browse/{t.JiraIssueKey}",
            t.CreatedAt
        ));
    }

    public async Task<bool> ValidateCredentialsAsync(string email, string apiToken, string siteUrl)
    {
        try
        {
            var client = CreateClient(email, apiToken, siteUrl);
            var response = await client.GetAsync("rest/api/3/myself");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private async Task<HttpClient> CreateClientAsync(int userId)
    {
        var (email, apiToken, siteUrl) = await _configService.GetCredentialsAsync(userId);
        return CreateClient(email, apiToken, siteUrl);
    }

    private HttpClient CreateClient(string email, string apiToken, string siteUrl)
    {
        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(siteUrl.TrimEnd('/') + "/");

        var authBytes = Encoding.UTF8.GetBytes($"{email}:{apiToken}");
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        return client;
    }

    public async Task<IEnumerable<BatchTicketResult>> CreateTicketsBulkWithCredentialsAsync(
        string email, string apiToken, string siteUrl, IList<CreateTicketRequest> requests)
    {
        var client = CreateClient(email, apiToken, siteUrl);

        var issueUpdates = requests.Select(r =>
        {
            var fields = BuildFieldsDictionary(r);
            return new { fields };
        }).ToList();

        var payload = new { issueUpdates };
        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await client.PostAsync("rest/api/3/issue/bulk", content);

        var resultJson = await response.Content.ReadFromJsonAsync<JsonElement>();
        var results = new List<BatchTicketResult>();

        if (resultJson.TryGetProperty("issues", out var issues))
        {
            var createdIssues = issues.EnumerateArray().ToList();
            for (var i = 0; i < createdIssues.Count; i++)
            {
                var issue = createdIssues[i];
                var issueKey = issue.GetProperty("key").GetString()!;
                var selfUrl = $"{siteUrl.TrimEnd('/')}/browse/{issueKey}";

                results.Add(new BatchTicketResult(
                    requests[i].Title, true,
                    new TicketResponse(issueKey, requests[i].Title, selfUrl, DateTime.UtcNow),
                    null));
            }
        }

        if (resultJson.TryGetProperty("errors", out var errors))
        {
            foreach (var error in errors.EnumerateArray())
            {
                var errorMessages = error.TryGetProperty("elementErrors", out var elementErrors)
                    && elementErrors.TryGetProperty("errors", out var errMap)
                        ? string.Join("; ", errMap.EnumerateObject().Select(e => $"{e.Name}: {e.Value}"))
                        : "Unknown Jira error";

                var failedIndex = error.TryGetProperty("failedElementNumber", out var num)
                    ? num.GetInt32()
                    : -1;

                var title = failedIndex >= 0 && failedIndex < requests.Count
                    ? requests[failedIndex].Title
                    : "Unknown";

                results.Add(new BatchTicketResult(title, false, null, errorMessages));
            }
        }

        return results;
    }

    private static Dictionary<string, object> BuildFieldsDictionary(CreateTicketRequest request)
    {
        var fields = new Dictionary<string, object>
        {
            ["project"] = new { key = request.ProjectKey },
            ["summary"] = request.Title,
            ["description"] = new
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
            ["issuetype"] = new { name = "Task" }
        };

        if (!string.IsNullOrWhiteSpace(request.AssigneeAccountId))
            fields["assignee"] = new { accountId = request.AssigneeAccountId };

        if (!string.IsNullOrWhiteSpace(request.Priority))
            fields["priority"] = new { name = request.Priority };

        return fields;
    }

    private static string? ExtractPlainText(JsonElement fields)
    {
        if (!fields.TryGetProperty("description", out var desc) || desc.ValueKind == JsonValueKind.Null)
            return null;

        return ExtractCommentText(desc);
    }

    private static string ExtractCommentText(JsonElement adfNode)
    {
        if (!adfNode.TryGetProperty("content", out var content))
            return string.Empty;

        var parts = new List<string>();
        foreach (var block in content.EnumerateArray())
        {
            if (block.TryGetProperty("content", out var inlineContent))
            {
                foreach (var inline in inlineContent.EnumerateArray())
                {
                    if (inline.TryGetProperty("text", out var text))
                        parts.Add(text.GetString()!);
                }
            }
        }
        return string.Join("\n", parts);
    }
}
