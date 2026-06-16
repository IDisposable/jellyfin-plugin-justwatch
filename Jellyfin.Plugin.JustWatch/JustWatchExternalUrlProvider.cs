using System.Collections.Generic;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Plugin.JustWatch;

/// <summary>
/// Emits the clickable JustWatch deep-link shown in an item's external links.
/// </summary>
/// <remarks>
/// Renders the item's own JustWatch id if it has one. Seasons and episodes have no JustWatch page of
/// their own, so their link is built from the parent series id and the season number.
/// </remarks>
public class JustWatchExternalUrlProvider : IExternalUrlProvider
{
    /// <inheritdoc />
    public string Name => JustWatchUtils.ProviderName;

    /// <inheritdoc />
    public IEnumerable<string> GetExternalUrls(BaseItem item)
    {
        // Use the item's own JustWatch id if present.
        if (item.TryGetProviderId(JustWatchUtils.ProviderName, out var id))
        {
            var url = JustWatchUtils.BuildUrl(id);
            if (url is not null)
            {
                yield return url;
                yield break;
            }
        }

        // Otherwise build a season link from the parent series.
        Series? series = null;
        int? seasonNumber = null;
        if (item is Season season)
        {
            series = season.Series;
            seasonNumber = season.IndexNumber;
        }
        else if (item is Episode episode)
        {
            series = episode.Series;
            seasonNumber = episode.ParentIndexNumber;
        }

        if (series is not null
            && seasonNumber is { } number
            && series.TryGetProviderId(JustWatchUtils.ProviderName, out var seriesId))
        {
            var seasonUrl = JustWatchUtils.BuildSeasonUrl(seriesId, number);
            if (seasonUrl is not null)
            {
                yield return seasonUrl;
            }
        }
    }
}
