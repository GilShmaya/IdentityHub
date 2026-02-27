using IdentityHub.Api.Data;
using IdentityHub.Api.DTOs;
using IdentityHub.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace IdentityHub.Tests;

public class ApiKeyServiceTests
{
    private static (AppDbContext db, ApiKeyService service) CreateService()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);

        // Seed a user
        db.Users.Add(new Api.Models.User
        {
            Id = 1,
            Email = "test@example.com",
            PasswordHash = "hash"
        });
        db.SaveChanges();

        return (db, new ApiKeyService(db));
    }

    [Fact]
    public async Task CreateKey_ReturnsRawKey()
    {
        var (_, service) = CreateService();
        var result = await service.CreateKeyAsync(1, new CreateApiKeyRequest("CI Scanner"));

        Assert.NotEmpty(result.Key);
        Assert.StartsWith("ih_", result.Key);
        Assert.Equal("CI Scanner", result.Name);
    }

    [Fact]
    public async Task ValidateKey_ValidKey_ReturnsUserId()
    {
        var (_, service) = CreateService();
        var created = await service.CreateKeyAsync(1, new CreateApiKeyRequest("Test Key"));

        var userId = await service.ValidateKeyAsync(created.Key);

        Assert.Equal(1, userId);
    }

    [Fact]
    public async Task ValidateKey_InvalidKey_ReturnsNull()
    {
        var (_, service) = CreateService();

        var userId = await service.ValidateKeyAsync("ih_invalid-key");

        Assert.Null(userId);
    }

    [Fact]
    public async Task RevokeKey_InvalidatesKey()
    {
        var (_, service) = CreateService();
        var created = await service.CreateKeyAsync(1, new CreateApiKeyRequest("Test Key"));

        await service.RevokeKeyAsync(1, created.Id);
        var userId = await service.ValidateKeyAsync(created.Key);

        Assert.Null(userId);
    }

    [Fact]
    public async Task GetKeys_ReturnsAllKeys()
    {
        var (_, service) = CreateService();
        await service.CreateKeyAsync(1, new CreateApiKeyRequest("Key 1"));
        await service.CreateKeyAsync(1, new CreateApiKeyRequest("Key 2"));

        var keys = (await service.GetKeysAsync(1)).ToList();

        Assert.Equal(2, keys.Count);
    }

    [Fact]
    public async Task RevokeKey_WrongUser_Throws()
    {
        var (_, service) = CreateService();
        var created = await service.CreateKeyAsync(1, new CreateApiKeyRequest("Test Key"));

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.RevokeKeyAsync(999, created.Id));
    }
}
