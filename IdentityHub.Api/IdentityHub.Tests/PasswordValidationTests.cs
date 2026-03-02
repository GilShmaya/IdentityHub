using IdentityHub.Api.DTOs;
using IdentityHub.Api.Services;
using Microsoft.Extensions.Configuration;
using IdentityHub.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace IdentityHub.Tests;

public class PasswordValidationTests
{
    private static AuthService CreateService()
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

        return new AuthService(db, config);
    }

    [Fact]
    public async Task Register_ShortPassword_Throws()
    {
        var service = CreateService();
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.RegisterAsync(new RegisterRequest("a@b.com", "Short1")));
        Assert.Contains("8 characters", ex.Message);
    }

    [Fact]
    public async Task Register_NoUppercase_Throws()
    {
        var service = CreateService();
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.RegisterAsync(new RegisterRequest("a@b.com", "lowercase1")));
        Assert.Contains("uppercase", ex.Message);
    }

    [Fact]
    public async Task Register_NoLowercase_Throws()
    {
        var service = CreateService();
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.RegisterAsync(new RegisterRequest("a@b.com", "UPPERCASE1")));
        Assert.Contains("lowercase", ex.Message);
    }

    [Fact]
    public async Task Register_NoDigit_Throws()
    {
        var service = CreateService();
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.RegisterAsync(new RegisterRequest("a@b.com", "NoDigitHere")));
        Assert.Contains("digit", ex.Message);
    }

    [Fact]
    public async Task Register_NoSpecialCharacter_Throws()
    {
        var service = CreateService();
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.RegisterAsync(new RegisterRequest("a@b.com", "V4lidPass")));
        Assert.Contains("special character", ex.Message);
    }

    [Fact]
    public async Task Register_ValidComplexPassword_Succeeds()
    {
        var service = CreateService();
        var result = await service.RegisterAsync(new RegisterRequest("a@b.com", "V4lidPass!"));
        Assert.NotEmpty(result.Token);
    }
}
