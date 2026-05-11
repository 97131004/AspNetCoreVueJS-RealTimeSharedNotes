using AspNetCore_RealTimeSharedNotes.Data.Interfaces;
using AspNetCore_RealTimeSharedNotes.Models;
using Microsoft.EntityFrameworkCore;

namespace AspNetCore_RealTimeSharedNotes.Data;

public class ApiKeyRepository : BaseRepository, IApiKeyRepository
{
    public ApiKeyRepository(ApplicationDbContext db) : base(db) { }

    public async Task<ApiKey?> GetApiKeyAsync(string clientId)
    {
        return await _db.ApiKeys.FirstOrDefaultAsync(a => a.ClientId == clientId);
    }

    public async Task<ApiKey> AddApiKeyAsync(ApiKey apiKey)
    {
        _db.ApiKeys.Add(apiKey);
        await _db.SaveChangesAsync();
        return apiKey;
    }

    public async Task<bool> DeleteApiKeyAsync(string userId)
    {
        var deleted = await _db.ApiKeys
            .Where(a => a.UserId == userId)
            .ExecuteDeleteAsync();
        return deleted > 0;
    }
}
