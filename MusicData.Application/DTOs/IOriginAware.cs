namespace MusicData.Application.DTOs;

public interface IOriginAware
{
    string Origin { get; set; }

    /// <summary>
    /// True when a secondary provider failed while this DTO was built, so it
    /// may be missing data: it is persisted with a short time-to-live so the
    /// missing part is retried soon. Never serialized.
    /// </summary>
    bool IsPartial { get; set; }
}
