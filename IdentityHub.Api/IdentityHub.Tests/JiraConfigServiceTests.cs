using IdentityHub.Api.Data;
using IdentityHub.Api.DTOs;
using IdentityHub.Api.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace IdentityHub.Tests;

public class JiraConfigServiceTests
{
    private static (AppDbContext db, JiraConfigService service) CreateService()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);
        var dataProtection = DataProtectionProvider.Create("Tests");
        return (db, new JiraConfigService(db, dataProtection));
    }

    [Fact]
    public async Task SaveConfig_NewUser_CreatesConfig()
    {
        var (db, service) = CreateService();
        await service.SaveConfigAsync(1, new SaveJiraConfigRequest("jira@test.com", "token123", "https://test.atlassian.net"));

        var config = await db.JiraConfigurations.FirstOrDefaultAsync(j => j.UserId == 1);
        Assert.NotNull(config);
        Assert.Equal("jira@test.com", config.Email);
        Assert.Equal("https://test.atlassian.net", config.SiteUrl);
    }

    [Fact]
    public async Task SaveConfig_ExistingUser_UpdatesConfig()
    {
        var (db, service) = CreateService();
        await service.SaveConfigAsync(1, new SaveJiraConfigRequest("old@test.com", "token1", "https://old.atlassian.net"));
        await service.SaveConfigAsync(1, new SaveJiraConfigRequest("new@test.com", "token2", "https://new.atlassian.net"));

        Assert.Equal(1, await db.JiraConfigurations.CountAsync());
        var config = await db.JiraConfigurations.FirstAsync(j => j.UserId == 1);
        Assert.Equal("new@test.com", config.Email);
    }

    [Fact]
    public async Task GetConfig_NoConfig_ReturnsNotConfigured()
    {
        var (_, service) = CreateService();
        var result = await service.GetConfigAsync(999);

        Assert.False(result.IsConfigured);
        Assert.Empty(result.Email);
    }

    [Fact]
    public async Task GetConfig_HasConfig_ReturnsConfigured()
    {
        var (_, service) = CreateService();
        await service.SaveConfigAsync(1, new SaveJiraConfigRequest("jira@test.com", "token123", "https://test.atlassian.net"));

        var result = await service.GetConfigAsync(1);

        Assert.True(result.IsConfigured);
        Assert.Equal("jira@test.com", result.Email);
        Assert.Equal("https://test.atlassian.net", result.SiteUrl);
    }

    [Fact]
    public async Task GetCredentials_NoConfig_Throws()
    {
        var (_, service) = CreateService();
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetCredentialsAsync(999));
    }

    [Fact]
    public async Task GetCredentials_HasConfig_DecryptsToken()
    {
        var (_, service) = CreateService();
        await service.SaveConfigAsync(1, new SaveJiraConfigRequest("jira@test.com", "my-secret-token", "https://test.atlassian.net"));

        var (email, apiToken, siteUrl) = await service.GetCredentialsAsync(1);

        Assert.Equal("jira@test.com", email);
        Assert.Equal("my-secret-token", apiToken);
        Assert.Equal("https://test.atlassian.net", siteUrl);
    }

    [Fact]
    public async Task GetConfig_DifferentUsers_Isolated()
    {
        var (_, service) = CreateService();
        await service.SaveConfigAsync(1, new SaveJiraConfigRequest("user1@test.com", "token1", "https://site1.atlassian.net"));
        await service.SaveConfigAsync(2, new SaveJiraConfigRequest("user2@test.com", "token2", "https://site2.atlassian.net"));

        var config1 = await service.GetConfigAsync(1);
        var config2 = await service.GetConfigAsync(2);

        Assert.Equal("user1@test.com", config1.Email);
        Assert.Equal("user2@test.com", config2.Email);
    }

    [Fact]
    public async Task SaveConfig_TrimsTrailingSlash()
    {
        var (_, service) = CreateService();
        await service.SaveConfigAsync(1, new SaveJiraConfigRequest("jira@test.com", "token", "https://test.atlassian.net/"));

        var result = await service.GetConfigAsync(1);
        Assert.Equal("https://test.atlassian.net", result.SiteUrl);
    }
}
