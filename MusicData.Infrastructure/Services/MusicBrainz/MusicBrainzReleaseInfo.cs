namespace MusicData.Infrastructure.Services.MusicBrainz;

public partial class MusicBrainzService
{
    public sealed class MusicBrainzReleaseInfo
    {
        public string? ReleaseId { get; set; }

        public string? ReleaseGroupId { get; set; }
    }
}

