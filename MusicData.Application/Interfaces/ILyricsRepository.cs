using MusicData.Domain.Entities;

namespace MusicData.Application.Interfaces;

public interface ILyricsRepository
{
    LyricsEntity? Get(string title, string artistName);

    void Add(LyricsEntity lyrics);
}
