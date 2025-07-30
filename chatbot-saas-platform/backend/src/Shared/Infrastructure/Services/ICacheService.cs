using System;
using System.Threading.Tasks;

namespace Shared.Infrastructure.Services;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key) where T : class;
    Task<T?> GetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null) where T : class;
    Task RemoveAsync(string key);
    Task RemoveByPatternAsync(string pattern);
    Task<bool> ExistsAsync(string key);
    Task RefreshAsync(string key);
}
