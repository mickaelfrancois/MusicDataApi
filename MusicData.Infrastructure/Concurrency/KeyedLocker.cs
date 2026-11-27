using MusicData.Application.Interfaces;

namespace MusicData.Infrastructure.Concurrency;

internal sealed class KeyedLocker : IKeyedLocker
{
    private readonly Dictionary<string, Entry> _entries = new(StringComparer.Ordinal);
    private readonly object _gate = new();

    /// <summary>Number of keys currently held or awaited (entries are dropped once idle).</summary>
    internal int ActiveKeyCount
    {
        get { lock (_gate) return _entries.Count; }
    }

    public async Task<IDisposable> LockAsync(string key, CancellationToken cancellationToken = default)
    {
        Entry entry;
        lock (_gate)
        {
            if (!_entries.TryGetValue(key, out Entry? existing))
            {
                existing = new Entry();
                _entries[key] = existing;
            }

            existing.Users++;
            entry = existing;
        }

        try
        {
            await entry.Semaphore.WaitAsync(cancellationToken);
        }
        catch
        {
            Release(key, entry, acquired: false);
            throw;
        }

        return new Releaser(this, key, entry);
    }

    // Users counts holders + waiters, so an entry is only removed when nobody
    // can still be using its semaphore. Without this the dictionary grew by one
    // semaphore per distinct (user-supplied) key, forever.
    private void Release(string key, Entry entry, bool acquired)
    {
        if (acquired)
            entry.Semaphore.Release();

        lock (_gate)
        {
            if (--entry.Users == 0)
                _entries.Remove(key);
        }
    }

    private sealed class Entry
    {
        public SemaphoreSlim Semaphore { get; } = new(1, 1);

        public int Users { get; set; }
    }

    private sealed class Releaser(KeyedLocker owner, string key, Entry entry) : IDisposable
    {
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
                owner.Release(key, entry, acquired: true);
        }
    }
}
