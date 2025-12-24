namespace MusicData.Application.DTOs;

public class TrackDto
{
    public string Name { get; set; } = string.Empty;

    public int Position { get; set; }

    public int? Duration { get; set; }

    public override string ToString() => $"{Position}. {Name}";
}