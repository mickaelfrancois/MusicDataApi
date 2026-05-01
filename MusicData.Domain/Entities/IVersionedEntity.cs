namespace MusicData.Domain.Entities;

public interface IVersionedEntity
{
    int Id { get; set; }

    int Version { get; set; }

    DateTime UpdateDateTime { get; set; }
}
