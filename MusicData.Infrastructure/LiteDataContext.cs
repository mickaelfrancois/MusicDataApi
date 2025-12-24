using LiteDB;
using MusicData.Application.Interfaces;

namespace MusicData.Infrastructure;

internal sealed class LiteDataContext : IDataContext, IDisposable
{
    private readonly ILiteDatabase _database;

    public LiteDataContext(ILiteDatabase database)
    {
        _database = database;
    }

    public void Dispose() => _database?.Dispose();
}
