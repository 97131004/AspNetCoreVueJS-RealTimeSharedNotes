using AspNetCore_RealTimeSharedNotes.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AspNetCore_RealTimeSharedNotes.Data;

public abstract class BaseRepository : IBaseRepository
{
    protected readonly ApplicationDbContext _db;

    protected BaseRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    //auto-rollbacks every db operation (within action) if an exception is thrown, otherwise commits
    public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> action)
    {
        await using (var transaction = await _db.Database.BeginTransactionAsync())
        {
            try
            {
                var result = await action();
                await transaction.CommitAsync();
                return result;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
