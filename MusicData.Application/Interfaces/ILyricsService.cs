using MusicData.Application.DTOs;

namespace MusicData.Application.Interfaces;

public interface ILyricsService
{
    bool Enabled { get; set; }

    Task<LyricsDto?> GetLyricsAsync(string title, string artistName, string albumName, int duration, CancellationToken cancellationToken);
}