using IdentityHub.Api.Data;
using IdentityHub.Api.DTOs;
using IdentityHub.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace IdentityHub.Tests;

public class AuthServiceTests
{
    private static (AppDbContext db, AuthService service) CreateService()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "test-secret-key-that-is-at-least-32-characters-long!!",
                ["Jwt:Issuer"] = "TestIssuer",
                ["Jwt:Audience"] = "TestAudience"
            })
            .Build();

        return (db, new AuthService(db, config));
    }

    [Fact]
    public async Task Register_CreatesUser_ReturnsToken()
    {
        var (db, service) = CreateService();
        var result = await service.RegisterAsync(new RegisterRequest("test@example.com", "Password1!"));

        Assert.NotEmpty(result.Token);
        Assert.Equal("test@example.com", result.Email);
        Assert.Single(await db.Users.ToListAsync());
    }

    [Fact]
    public async Task Register_DuplicateEmail_Throws()
    {
        var (_, service) = CreateService();
        await service.RegisterAsync(new RegisterRequest("test@example.com", "Password1!"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.RegisterAsync(new RegisterRequest("test@example.com", "OtherPass1!")));
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsToken()
    {
        var (_, service) = CreateService();
        await service.RegisterAsync(new RegisterRequest("test@example.com", "Password1!"));

        var result = await service.LoginAsync(new LoginRequest("test@example.com", "Password1!"));

        Assert.NotEmpty(result.Token);
        Assert.Equal("test@example.com", result.Email);
    }

    [Fact]
    public async Task Login_WrongPassword_Throws()
    {
        var (_, service) = CreateService();
        await service.RegisterAsync(new RegisterRequest("test@example.com", "Password1!"));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.LoginAsync(new LoginRequest("test@example.com", "WrongPass1!")));
    }

    [Fact]
    public async Task Login_NonExistentUser_Throws()
    {
        var (_, service) = CreateService();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.LoginAsync(new LoginRequest("noone@example.com", "Password1")));
    }
}
