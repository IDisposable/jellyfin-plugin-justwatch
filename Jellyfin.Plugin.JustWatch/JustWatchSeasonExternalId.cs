using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.JustWatch;

/// <summary>
/// JustWatch external id for seasons (distinct season pages, e.g. /us/tv-show/x/season-1).
/// </summary>
public class JustWatchSeasonExternalId : JustWatchExternalId
{
    /// <inheritdoc />
    public override ExternalIdMediaType? Type => ExternalIdMediaType.Season;

    /// <inheritdoc />
    public override bool Supports(IHasProviderIds item) => item is Season;
}
