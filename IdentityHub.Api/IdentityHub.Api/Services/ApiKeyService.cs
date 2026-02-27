using System.Security.Cryptography;
using IdentityHub.Api.Data;
using IdentityHub.Api.DTOs;
using IdentityHub.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace IdentityHub.Api.Services;

public class ApiKeyService : IApiKeyService
{
    private const int KEY_LENGTH = 32;
    private const string KEY_PREFIX = "ih_";
    private readonly AppDbContext _db;

    public ApiKeyService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ApiKeyCreatedResponse> CreateKeyAsync(int userId, CreateApiKeyRequest request)
    {
        var rawKey = KEY_PREFIX + Convert.ToBase64String(RandomNumberGenerator.GetBytes(KEY_LENGTH));
        var keyHash = HashKey(rawKey);
        var prefix = rawKey[..Math.Min(12, rawKey.Length)];

        var apiKey = new ApiKey
        {
            UserId = userId,
            Name = request.Name,
            KeyHash = keyHash,
            KeyPrefix = prefix,
            CreatedAt = DateTime.UtcNow
        };

        _db.ApiKeys.Add(apiKey);
        await _db.SaveChangesAsync();

        return new ApiKeyCreatedResponse(apiKey.Id, apiKey.Name, rawKey, apiKey.CreatedAt);
    }

    public async Task<IEnumerable<ApiKeyResponse>> GetKeysAsync(int userId)
    {
        return await _db.ApiKeys
            .Where(k => k.UserId == userId)
            .OrderByDescending(k => k.CreatedAt)
            .Select(k => new ApiKeyResponse(k.Id, k.Name, k.KeyPrefix, k.CreatedAt, k.IsRevoked))
            .ToListAsync();
    }

    public async Task RevokeKeyAsync(int userId, int keyId)
    {
        var key = await _db.ApiKeys.FirstOrDefaultAsync(k => k.Id == keyId && k.UserId == userId)
            ?? throw new KeyNotFoundException("API key not found.");

        key.IsRevoked = true;
        await _db.SaveChangesAsync();
    }

    public async Task<int?> ValidateKeyAsync(string rawKey)
    {
        var keyHash = HashKey(rawKey);
        var apiKey = await _db.ApiKeys.FirstOrDefaultAsync(k => k.KeyHash == keyHash && !k.IsRevoked);
        return apiKey?.UserId;
    }

    private static string HashKey(string rawKey)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(rawKey);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }
}
