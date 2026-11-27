namespace MusicData.Domain.Entities;

public interface IVersionedEntity
{
    int Id { get; set; }

    int Version { get; set; }

    DateTime UpdateDateTime { get; set; }

    /// <summary>
    /// True when a secondary provider failed while the row was built: it may
    /// miss data, so it is cached only briefly and refetched soon.
    /// </summary>
    bool IsPartial { get; set; }
}
