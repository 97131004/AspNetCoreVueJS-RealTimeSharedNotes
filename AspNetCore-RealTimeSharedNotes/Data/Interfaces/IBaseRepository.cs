namespace AspNetCore_RealTimeSharedNotes.Data.Interfaces;

public interface IBaseRepository
{
    Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> action);
}
