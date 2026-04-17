namespace Motivation.Data
{
    public interface IRepository<T> where T : class
    {
        IQueryable<T> Entries { get; }
        Task CreateAsync(T entry);
        Task UpdateAsync(T entry);
        Task DeleteAsync(int entryId);
    }
}
