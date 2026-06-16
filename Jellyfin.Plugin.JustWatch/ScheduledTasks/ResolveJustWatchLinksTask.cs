using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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

    /// <summary>Flush pending library writes — and emit a progress log line — every this many items.</summary>
    private const int BatchSize = 25;

    /// <summary>One-shot flag: when set, the next run re-queries items the negative cache would skip.</summary>
    private static int _forceRun;

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
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers() => new[]
    {
        // Runs weekly; no-ops unless ResolveLinksEnabled. Editable in Dashboard > Scheduled Tasks.
        new TaskTriggerInfo
        {
            Type = TaskTriggerInfoType.WeeklyTrigger,
            DayOfWeek = DayOfWeek.Sunday,
            TimeOfDayTicks = TimeSpan.FromHours(3).Ticks
        }
    };

    /// <summary>
    /// Requests that the next run re-query items the negative cache would otherwise skip (used by the
    /// "re-resolve" action). Consumed once, on the next run.
    /// </summary>
    public static void RequestForceRun() => Interlocked.Exchange(ref _forceRun, 1);

    /// <inheritdoc />
    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        var config = Plugin.Instance?.Configuration ?? new PluginConfiguration();
        if (!config.ResolveLinksEnabled)
        {
            _logger.LogInformation("JustWatch link resolution is disabled; enable it in the plugin settings to run this task.");
            return;
        }

        var force = Interlocked.Exchange(ref _forceRun, 0) == 1;
        if (force)
        {
            _logger.LogInformation("JustWatch: forced run — re-querying items the unmatched cache would normally skip.");
        }

        var requestDelay = TimeSpan.FromMilliseconds(Math.Max(0, config.RequestDelayMs));

        var items = _libraryManager.GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = new[] { BaseItemKind.Movie, BaseItemKind.Series },
            Recursive = true
        });

        _logger.LogInformation(
            "JustWatch link resolution started: {Count} movies/series to scan, country {Country}, request delay {Delay}ms.",
            items.Count,
            config.Country,
            config.RequestDelayMs);

        var processed = 0;
        var stamped = 0;
        var skippedCached = 0;
        var pending = new List<BaseItem>();
        foreach (var item in items)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (pending.Count >= BatchSize)
            {
                await SaveBatchAsync(pending, cancellationToken).ConfigureAwait(false);
            }

            if (processed >= MaxItems)
            {
                _logger.LogInformation("JustWatch: reached item cap ({Cap}); run again to continue", MaxItems);
                break;
            }

            if (item.TryGetProviderId(JustWatchUtils.ProviderName, out var existing) && !string.IsNullOrEmpty(existing))
            {
                continue;
            }

            // Negative cache: skip items that recently missed, until the recheck window elapses
            // (a forced run bypasses this).
            if (!force
                && item.TryGetProviderId(JustWatchUtils.CheckedProviderName, out var checkedMarker)
                && JustWatchUtils.ShouldSkipUnmatched(checkedMarker, config.RecheckUnmatchedDays, DateTime.UtcNow))
            {
                skippedCached++;
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
                // Record the miss so we don't re-query it every run (see RecheckUnmatchedDays).
                item.SetProviderId(JustWatchUtils.CheckedProviderName, DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
                pending.Add(item);
                continue;
            }

            item.SetProviderId(JustWatchUtils.ProviderName, fullPath);
            item.SetProviderId(JustWatchUtils.CheckedProviderName, string.Empty); // clear any prior miss marker
            pending.Add(item);
            stamped++;
            _logger.LogDebug("JustWatch: stamped {Name} -> {Path}", item.Name, fullPath);

            progress.Report(processed * 100.0 / Math.Min(items.Count, MaxItems));
            if (processed % BatchSize == 0)
            {
                _logger.LogInformation("JustWatch progress: searched {Processed}, stamped {Stamped}.", processed, stamped);
            }
        }

        await SaveBatchAsync(pending, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("JustWatch: deriving season ids from resolved series.");
        var seasonsStamped = await StampSeasonsAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "JustWatch link resolution complete: stamped {Stamped} of {Processed} searched items (skipped {Cached} cached misses), plus {Seasons} seasons",
            stamped,
            processed,
            skippedCached,
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
        var pending = new List<BaseItem>();
        foreach (var item in seasons)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (pending.Count >= BatchSize)
            {
                await SaveBatchAsync(pending, cancellationToken).ConfigureAwait(false);
            }

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
            pending.Add(season);
            stamped++;
            _logger.LogDebug("JustWatch: stamped season {Name} -> {Path}", season.Name, seasonId);
        }

        await SaveBatchAsync(pending, cancellationToken).ConfigureAwait(false);
        return stamped;
    }

    /// <summary>
    /// Persists a batch of modified items in one repository write, grouped by parent (the parent only
    /// drives child-cache invalidation, so grouping keeps that correct). Clears the batch afterwards.
    /// </summary>
    private async Task SaveBatchAsync(List<BaseItem> batch, CancellationToken cancellationToken)
    {
        if (batch.Count == 0)
        {
            return;
        }

        foreach (var group in batch.GroupBy(i => i.GetParent()))
        {
            await _libraryManager.UpdateItemsAsync(
                group.ToList(),
                group.Key,
                ItemUpdateType.MetadataEdit,
                cancellationToken).ConfigureAwait(false);
        }

        batch.Clear();
    }
}
