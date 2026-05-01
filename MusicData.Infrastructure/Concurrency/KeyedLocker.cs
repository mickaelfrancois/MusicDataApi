using System.Collections.Concurrent;
using MusicData.Application.Interfaces;

namespace MusicData.Infrastructure.Concurrency;

internal sealed class KeyedLocker : IKeyedLocker
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new(StringComparer.Ordinal);

    public async Task<IDisposable> LockAsync(string key, CancellationToken cancellationToken = default)
    {
        SemaphoreSlim semaphore = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync(cancellationToken);
        return new Releaser(semaphore);
    }

    private sealed class Releaser(SemaphoreSlim semaphore) : IDisposable
    {
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
                semaphore.Release();
        }
    }
}
