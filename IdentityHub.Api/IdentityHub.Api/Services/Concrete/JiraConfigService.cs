using IdentityHub.Api.Data;
using IdentityHub.Api.DTOs;
using IdentityHub.Api.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace IdentityHub.Api.Services;

/// <summary>Manages per-user Jira credentials with encryption via Data Protection.</summary>
public class JiraConfigService : IJiraConfigService
{
    private const string PURPOSE = "JiraApiToken";
    private readonly AppDbContext _db;
    private readonly IDataProtector _protector;

    public JiraConfigService(AppDbContext db, IDataProtectionProvider dataProtection)
    {
        _db = db;
        _protector = dataProtection.CreateProtector(PURPOSE);
    }

    public async Task SaveConfigAsync(int userId, SaveJiraConfigRequest request)
    {
        var existing = await _db.JiraConfigurations.FirstOrDefaultAsync(j => j.UserId == userId);

        if (existing is not null)
        {
            existing.Email = request.Email;
            existing.EncryptedApiToken = _protector.Protect(request.ApiToken);
            existing.SiteUrl = request.SiteUrl.TrimEnd('/');
        }
        else
        {
            _db.JiraConfigurations.Add(new JiraConfiguration
            {
                UserId = userId,
                Email = request.Email,
                EncryptedApiToken = _protector.Protect(request.ApiToken),
                SiteUrl = request.SiteUrl.TrimEnd('/')
            });
        }

        await _db.SaveChangesAsync();
    }

    public async Task<JiraConfigResponse> GetConfigAsync(int userId)
    {
        var config = await _db.JiraConfigurations.FirstOrDefaultAsync(j => j.UserId == userId);
        return config is null
            ? new JiraConfigResponse("", "", false)
            : new JiraConfigResponse(config.Email, config.SiteUrl, true);
    }

    public async Task<(string Email, string ApiToken, string SiteUrl)> GetCredentialsAsync(int userId)
    {
        var config = await _db.JiraConfigurations.FirstOrDefaultAsync(j => j.UserId == userId)
            ?? throw new InvalidOperationException("Jira is not configured. Please configure your Jira credentials first.");

        var token = _protector.Unprotect(config.EncryptedApiToken);
        return (config.Email, token, config.SiteUrl);
    }
}
