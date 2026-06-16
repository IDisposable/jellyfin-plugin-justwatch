using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.JustWatch;

/// <summary>
/// JustWatch external id for episodes.
/// </summary>
public class JustWatchEpisodeExternalId : JustWatchExternalId
{
    /// <inheritdoc />
    public override ExternalIdMediaType? Type => ExternalIdMediaType.Episode;

    /// <inheritdoc />
    public override bool Supports(IHasProviderIds item) => item is Episode;
}
