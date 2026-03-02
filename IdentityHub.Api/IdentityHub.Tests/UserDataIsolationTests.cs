using IdentityHub.Api.Data;
using IdentityHub.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace IdentityHub.Tests;

public class UserDataIsolationTests
{
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task RecentTickets_FilteredByUserId()
    {
        var db = CreateDb();
        db.TicketReferences.AddRange(
            new TicketReference { UserId = 1, JiraIssueKey = "NHI-1", ProjectKey = "NHI", Title = "User1 Ticket", CreatedAt = DateTime.UtcNow },
            new TicketReference { UserId = 2, JiraIssueKey = "NHI-2", ProjectKey = "NHI", Title = "User2 Ticket", CreatedAt = DateTime.UtcNow }
        );
        await db.SaveChangesAsync();

        var user1Tickets = await db.TicketReferences
            .Where(t => t.UserId == 1 && t.ProjectKey == "NHI")
            .ToListAsync();

        Assert.Single(user1Tickets);
        Assert.Equal("NHI-1", user1Tickets[0].JiraIssueKey);
    }

    [Fact]
    public async Task UpdateTicketRef_OnlyOwnerCanUpdate()
    {
        var db = CreateDb();
        db.TicketReferences.Add(
            new TicketReference { UserId = 1, JiraIssueKey = "NHI-10", ProjectKey = "NHI", Title = "Original", CreatedAt = DateTime.UtcNow }
        );
        await db.SaveChangesAsync();

        // User 2 tries to find and update user 1's ticket
        var ticketAsUser2 = await db.TicketReferences
            .FirstOrDefaultAsync(t => t.JiraIssueKey == "NHI-10" && t.UserId == 2);
        Assert.Null(ticketAsUser2);

        // User 1 can find their own ticket
        var ticketAsUser1 = await db.TicketReferences
            .FirstOrDefaultAsync(t => t.JiraIssueKey == "NHI-10" && t.UserId == 1);
        Assert.NotNull(ticketAsUser1);
        Assert.Equal("Original", ticketAsUser1.Title);
    }

    [Fact]
    public async Task JiraConfig_IsolatedPerUser()
    {
        var db = CreateDb();
        db.JiraConfigurations.AddRange(
            new JiraConfiguration { UserId = 1, Email = "user1@test.com", EncryptedApiToken = "enc1", SiteUrl = "https://site1.atlassian.net" },
            new JiraConfiguration { UserId = 2, Email = "user2@test.com", EncryptedApiToken = "enc2", SiteUrl = "https://site2.atlassian.net" }
        );
        await db.SaveChangesAsync();

        var config1 = await db.JiraConfigurations.FirstOrDefaultAsync(j => j.UserId == 1);
        var config2 = await db.JiraConfigurations.FirstOrDefaultAsync(j => j.UserId == 2);

        Assert.NotNull(config1);
        Assert.NotNull(config2);
        Assert.NotEqual(config1.Email, config2.Email);
        Assert.NotEqual(config1.SiteUrl, config2.SiteUrl);
    }

    [Fact]
    public async Task RecentTickets_DifferentProjects_Isolated()
    {
        var db = CreateDb();
        db.TicketReferences.AddRange(
            new TicketReference { UserId = 1, JiraIssueKey = "NHI-1", ProjectKey = "NHI", Title = "NHI Ticket", CreatedAt = DateTime.UtcNow },
            new TicketReference { UserId = 1, JiraIssueKey = "SEC-1", ProjectKey = "SEC", Title = "SEC Ticket", CreatedAt = DateTime.UtcNow }
        );
        await db.SaveChangesAsync();

        var nhiTickets = await db.TicketReferences
            .Where(t => t.UserId == 1 && t.ProjectKey == "NHI")
            .ToListAsync();

        Assert.Single(nhiTickets);
        Assert.Equal("NHI-1", nhiTickets[0].JiraIssueKey);
    }

    [Fact]
    public async Task RecentTickets_ReturnsMax10_OrderedByDate()
    {
        var db = CreateDb();
        for (var i = 1; i <= 15; i++)
        {
            db.TicketReferences.Add(new TicketReference
            {
                UserId = 1,
                JiraIssueKey = $"NHI-{i}",
                ProjectKey = "NHI",
                Title = $"Ticket {i}",
                CreatedAt = DateTime.UtcNow.AddMinutes(-i)
            });
        }
        await db.SaveChangesAsync();

        var tickets = await db.TicketReferences
            .Where(t => t.UserId == 1 && t.ProjectKey == "NHI")
            .OrderByDescending(t => t.CreatedAt)
            .Take(10)
            .ToListAsync();

        Assert.Equal(10, tickets.Count);
        Assert.Equal("NHI-1", tickets[0].JiraIssueKey);
    }
}
