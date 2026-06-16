using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.JustWatch;

/// <summary>
/// JustWatch external id for series (distinct series pages, e.g. /us/tv-show/x).
/// </summary>
public class JustWatchSeriesExternalId : JustWatchExternalId
{
    /// <inheritdoc />
    public override ExternalIdMediaType? Type => ExternalIdMediaType.Series;

    /// <inheritdoc />
    public override bool Supports(IHasProviderIds item) => item is Series;
}
