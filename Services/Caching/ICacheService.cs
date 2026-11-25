namespace Services
{
    public interface ICacheService
    {
        /// <summary>
        /// Returns the cached value for <paramref name="key"/>, or calls
        /// <paramref name="load"/> (the database), caches its result for
        /// <paramref name="ttl"/> and returns it.
        /// </summary>
        Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> load, TimeSpan ttl);

        Task<string?> GetStringAsync(string key);

        /// <summary>Stores a value with no expiry.</summary>
        Task SetStringAsync(string key, string value);
    }
}
