namespace MusicData.Application.DTOs;

public interface IOriginAware
{
    string Origin { get; set; }

    /// <summary>
    /// True when at least one provider failed while this DTO was built, so it
    /// may be missing data and must not be persisted. Never serialized.
    /// </summary>
    bool IsPartial { get; set; }
}
