using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.JustWatch.Configuration;
using Jellyfin.Plugin.JustWatch.Graphql;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.JustWatch.ScheduledTasks;

/// <summary>
/// Opt-in task that resolves and stamps <c>ProviderIds["JustWatch"]</c> on movies and series that
/// don't have one yet, via the (unofficial) JustWatch GraphQL API. Manual trigger only; no-ops
/// unless <see cref="PluginConfiguration.ResolveLinksEnabled"/> is set.
/// </summary>
public sealed class ResolveJustWatchLinksTask : IScheduledTask
{
    private const int MaxItems = 500;

    private readonly ILibraryManager _libraryManager;
    private readonly JustWatchGraphQlClient _client;
    private readonly ILogger<ResolveJustWatchLinksTask> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResolveJustWatchLinksTask"/> class.
    /// </summary>
    /// <param name="libraryManager">The library manager.</param>
    /// <param name="client">The JustWatch GraphQL client.</param>
    /// <param name="logger">The logger.</param>
    public ResolveJustWatchLinksTask(
        ILibraryManager libraryManager,
        JustWatchGraphQlClient client,
        ILogger<ResolveJustWatchLinksTask> logger)
    {
        _libraryManager = libraryManager;
        _client = client;
        _logger = logger;
    }

    /// <inheritdoc />
    public string Name => "Resolve JustWatch links";

    /// <inheritdoc />
    public string Key => "JustWatchResolveLinks";

    /// <inheritdoc />
    public string Description => "Looks up the JustWatch page for movies/series missing a JustWatch id and stamps it (opt-in; unofficial API).";

    /// <inheritdoc />
    public string Category => "JustWatch";

    /// <inheritdoc />
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers() => Array.Empty<TaskTriggerInfo>();

    /// <inheritdoc />
    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        var config = Plugin.Instance?.Configuration ?? new PluginConfiguration();
        if (!config.ResolveLinksEnabled)
        {
            _logger.LogInformation("JustWatch link resolution is disabled; enable it in the plugin settings to run this task.");
            return;
        }

        var requestDelay = TimeSpan.FromMilliseconds(Math.Max(0, config.RequestDelayMs));

        var items = _libraryManager.GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = new[] { BaseItemKind.Movie, BaseItemKind.Series },
            Recursive = true
        });

        var processed = 0;
        var stamped = 0;
        foreach (var item in items)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (processed >= MaxItems)
            {
                _logger.LogInformation("JustWatch: reached item cap ({Cap}); run again to continue", MaxItems);
                break;
            }

            if (item.TryGetProviderId(JustWatchUtils.ProviderName, out var existing) && !string.IsNullOrEmpty(existing))
            {
                continue;
            }

            int? tmdbId = null;
            if (item.TryGetProviderId(MetadataProvider.Tmdb, out var tmdbStr)
                && int.TryParse(tmdbStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedTmdb))
            {
                tmdbId = parsedTmdb;
            }

            item.TryGetProviderId(MetadataProvider.Imdb, out var imdbId);

            if (!tmdbId.HasValue && string.IsNullOrEmpty(imdbId) && string.IsNullOrEmpty(item.Name))
            {
                continue;
            }

            if (processed > 0 && requestDelay > TimeSpan.Zero)
            {
                await Task.Delay(requestDelay, cancellationToken).ConfigureAwait(false);
            }

            processed++;

            var objectType = item.GetBaseItemKind() == BaseItemKind.Series ? "SHOW" : "MOVIE";
            var fullPath = await _client.ResolveFullPathAsync(
                item.Name,
                item.ProductionYear,
                tmdbId,
                imdbId,
                objectType,
                config.Country,
                config.Language,
                cancellationToken).ConfigureAwait(false);

            if (string.IsNullOrEmpty(fullPath))
            {
                continue;
            }

            item.SetProviderId(JustWatchUtils.ProviderName, fullPath);
            await _libraryManager.UpdateItemAsync(item, item.GetParent(), ItemUpdateType.MetadataEdit, cancellationToken).ConfigureAwait(false);
            stamped++;

            progress.Report(processed * 100.0 / Math.Min(items.Count, MaxItems));
        }

        var seasonsStamped = await StampSeasonsAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "JustWatch link resolution complete: stamped {Stamped} of {Processed} processed items, plus {Seasons} seasons",
            stamped,
            processed,
            seasonsStamped);
    }

    /// <summary>
    /// Derives and stamps season ids for seasons whose series already has a JustWatch id. A season's
    /// page is the series fullPath plus "/season-N", so no network call is needed. Specials and
    /// unnumbered seasons (index &lt; 1) are skipped, as is any season that already has an id.
    /// </summary>
    private async Task<int> StampSeasonsAsync(CancellationToken cancellationToken)
    {
        var seasons = _libraryManager.GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = new[] { BaseItemKind.Season },
            Recursive = true
        });

        var stamped = 0;
        foreach (var item in seasons)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (item is not Season season
                || (season.IndexNumber is not { } number) || number < 1)
            {
                continue;
            }

            if (season.TryGetProviderId(JustWatchUtils.ProviderName, out var existing) && !string.IsNullOrEmpty(existing))
            {
                continue;
            }

            if (season.Series is not { } series
                || !series.TryGetProviderId(JustWatchUtils.ProviderName, out var seriesPath)
                || string.IsNullOrEmpty(seriesPath))
            {
                continue;
            }

            var seasonId = JustWatchUtils.BuildSeasonId(seriesPath, number);
            if (string.IsNullOrEmpty(seasonId))
            {
                continue;
            }

            season.SetProviderId(JustWatchUtils.ProviderName, seasonId);
            await _libraryManager.UpdateItemAsync(season, season.GetParent(), ItemUpdateType.MetadataEdit, cancellationToken).ConfigureAwait(false);
            stamped++;
        }

        return stamped;
    }
}
