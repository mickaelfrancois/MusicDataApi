using MusicData.Infrastructure.Concurrency;

namespace MusicData.Tests.Infrastructure.Concurrency;

public class KeyedLockerTests
{
    [Fact]
    public async Task SameKey_SecondAcquire_BlocksUntilFirstReleased()
    {
        KeyedLocker locker = new();

        IDisposable first = await locker.LockAsync("k");

        Task<IDisposable> second = locker.LockAsync("k");

        // Give the runtime a moment; second must NOT have completed yet.
        await Task.Delay(50);
        Assert.False(second.IsCompleted, "Second LockAsync on the same key should block while the first is held.");

        first.Dispose();

        IDisposable secondAcquired = await second.WaitAsync(TimeSpan.FromSeconds(2));
        secondAcquired.Dispose();
    }

    [Fact]
    public async Task DifferentKeys_DoNotBlockEachOther()
    {
        KeyedLocker locker = new();

        IDisposable a = await locker.LockAsync("a");
        IDisposable b = await locker.LockAsync("b").WaitAsync(TimeSpan.FromSeconds(1));

        // Both acquired without waiting for each other.
        a.Dispose();
        b.Dispose();
    }

    [Fact]
    public async Task Dispose_IsIdempotent()
    {
        KeyedLocker locker = new();
        IDisposable handle = await locker.LockAsync("k");

        handle.Dispose();
        handle.Dispose(); // should not double-release the underlying semaphore

        // If the second Dispose had released the semaphore, the count would now be 2,
        // and a new Lock would still succeed but a SECOND new Lock would unexpectedly
        // also succeed without blocking. Verify a single re-acquire works AND a
        // concurrent attempt blocks.
        IDisposable handle2 = await locker.LockAsync("k");
        Task<IDisposable> blocked = locker.LockAsync("k");
        await Task.Delay(50);
        Assert.False(blocked.IsCompleted);

        handle2.Dispose();
        IDisposable third = await blocked.WaitAsync(TimeSpan.FromSeconds(2));
        third.Dispose();
    }

    [Fact]
    public async Task Cancellation_WhileWaiting_ThrowsOperationCanceled()
    {
        KeyedLocker locker = new();
        using IDisposable held = await locker.LockAsync("k");

        using CancellationTokenSource cts = new();
        Task<IDisposable> waiter = locker.LockAsync("k", cts.Token);

        cts.CancelAfter(TimeSpan.FromMilliseconds(50));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waiter);
    }

    [Fact]
    public async Task ReleasedKeys_AreRemoved_SoTheDictionaryDoesNotGrow()
    {
        KeyedLocker locker = new();

        for (int i = 0; i < 100; i++)
        {
            using IDisposable _ = await locker.LockAsync($"key-{i}");
        }

        Assert.Equal(0, locker.ActiveKeyCount);
    }

    [Fact]
    public async Task CancelledWaiter_DoesNotLeakItsEntry_NorBreakTheHolder()
    {
        KeyedLocker locker = new();
        IDisposable holder = await locker.LockAsync("k");

        using CancellationTokenSource cts = new();
        Task<IDisposable> waiter = locker.LockAsync("k", cts.Token);
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waiter);

        Assert.Equal(1, locker.ActiveKeyCount);
        holder.Dispose();
        Assert.Equal(0, locker.ActiveKeyCount);

        using IDisposable again = await locker.LockAsync("k").WaitAsync(TimeSpan.FromSeconds(1));
    }
}
