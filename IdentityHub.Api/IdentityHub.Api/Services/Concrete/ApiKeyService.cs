using System.Security.Cryptography;
using System.Text;
using IdentityHub.Api.Data;
using IdentityHub.Api.DTOs;
using IdentityHub.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace IdentityHub.Api.Services;

public class ApiKeyService : IApiKeyService
{
    private const int KEY_LENGTH = 32;
    private const int PREFIX_LENGTH = 8;
    private readonly AppDbContext _db;

    public ApiKeyService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<CreateApiKeyResponse> CreateAsync(int userId, string name)
    {
        var rawKey = GenerateRawKey();
        var prefix = rawKey[..PREFIX_LENGTH];
        var hash = HashKey(rawKey);

        var apiKey = new ApiKey
        {
            UserId = userId,
            Name = name,
            KeyHash = hash,
            KeyPrefix = prefix,
            CreatedAt = DateTime.UtcNow
        };

        _db.ApiKeys.Add(apiKey);
        await _db.SaveChangesAsync();

        return new CreateApiKeyResponse(apiKey.Id, apiKey.Name, rawKey, prefix, apiKey.CreatedAt);
    }

    public async Task<int?> ValidateAsync(string rawKey)
    {
        if (string.IsNullOrWhiteSpace(rawKey) || rawKey.Length < PREFIX_LENGTH)
            return null;

        var prefix = rawKey[..PREFIX_LENGTH];
        var candidates = await _db.ApiKeys
            .Where(k => k.KeyPrefix == prefix && !k.IsRevoked)
            .ToListAsync();

        var hash = HashKey(rawKey);
        var match = candidates.FirstOrDefault(k => k.KeyHash == hash);
        return match?.UserId;
    }

    public async Task RevokeAsync(int userId, int keyId)
    {
        var key = await _db.ApiKeys.FirstOrDefaultAsync(k => k.Id == keyId && k.UserId == userId)
            ?? throw new InvalidOperationException("API key not found.");

        key.IsRevoked = true;
        await _db.SaveChangesAsync();
    }

    public async Task<IEnumerable<ApiKeyResponse>> ListByUserAsync(int userId)
    {
        return await _db.ApiKeys
            .Where(k => k.UserId == userId)
            .OrderByDescending(k => k.CreatedAt)
            .Select(k => new ApiKeyResponse(k.Id, k.Name, k.KeyPrefix, k.CreatedAt, k.IsRevoked))
            .ToListAsync();
    }

    private static string GenerateRawKey()
    {
        var bytes = RandomNumberGenerator.GetBytes(KEY_LENGTH);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }

    private static string HashKey(string rawKey)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawKey));
        return Convert.ToHexStringLower(bytes);
    }
}
