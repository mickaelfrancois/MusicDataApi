namespace MusicData.Application.Interfaces;

public interface IKeyedLocker
{
    Task<IDisposable> LockAsync(string key, CancellationToken cancellationToken = default);
}
