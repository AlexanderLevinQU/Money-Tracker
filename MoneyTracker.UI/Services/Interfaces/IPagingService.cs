namespace MoneyTracker.UI.Services.Interfaces;

public interface IPagingService<T>
{
    void Reset(IEnumerable<T> items, int pageSize);
    List<T> GetNextPage();
    bool HasMore { get; }
}
