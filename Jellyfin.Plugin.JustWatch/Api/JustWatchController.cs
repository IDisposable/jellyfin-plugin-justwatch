using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.JustWatch.ScheduledTasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.JustWatch.Api;

/// <summary>
/// Admin endpoints backing the JustWatch settings page: resolver coverage counts and a forced re-run.
/// </summary>
[ApiController]
[Authorize(Policy = "RequiresElevation")]
[Route("Plugins/JustWatch")]
public class JustWatchController : ControllerBase
{
    private readonly ILibraryManager _libraryManager;
    private readonly ITaskManager _taskManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="JustWatchController"/> class.
    /// </summary>
    /// <param name="libraryManager">The library manager.</param>
    /// <param name="taskManager">The task manager.</param>
    public JustWatchController(ILibraryManager libraryManager, ITaskManager taskManager)
    {
        _libraryManager = libraryManager;
        _taskManager = taskManager;
    }

    /// <summary>
    /// Counts how many movies/series are matched, unmatched (negative-cached), or not yet searched.
    /// </summary>
    /// <returns>The resolver coverage counts.</returns>
    [HttpGet("Counts")]
    public ActionResult<JustWatchCounts> GetCounts()
    {
        var items = _libraryManager.GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = new[] { BaseItemKind.Movie, BaseItemKind.Series },
            Recursive = true
        });

        var counts = new JustWatchCounts { Total = items.Count };
        foreach (var item in items)
        {
            if (item.TryGetProviderId(JustWatchUtils.ProviderName, out var id) && !string.IsNullOrEmpty(id))
            {
                counts.Matched++;
            }
            else if (item.TryGetProviderId(JustWatchUtils.CheckedProviderName, out var marker) && !string.IsNullOrEmpty(marker))
            {
                counts.Unmatched++;
            }
            else
            {
                counts.Unqueried++;
            }
        }

        return counts;
    }

    /// <summary>
    /// Lists the movies/series the resolver searched but couldn't match (negative-cached), so they can
    /// be matched by hand. Ordered by name.
    /// </summary>
    /// <returns>The unmatched items.</returns>
    [HttpGet("Unmatched")]
    public ActionResult<IReadOnlyList<JustWatchUnmatchedItem>> GetUnmatched()
    {
        var items = _libraryManager.GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = new[] { BaseItemKind.Movie, BaseItemKind.Series },
            Recursive = true
        });

        var unmatched = new List<JustWatchUnmatchedItem>();
        foreach (var item in items)
        {
            if (item.TryGetProviderId(JustWatchUtils.ProviderName, out var id) && !string.IsNullOrEmpty(id))
            {
                continue;
            }

            if (!item.TryGetProviderId(JustWatchUtils.CheckedProviderName, out var marker) || string.IsNullOrEmpty(marker))
            {
                continue;
            }

            unmatched.Add(new JustWatchUnmatchedItem
            {
                ItemId = item.Id,
                Name = item.Name,
                ProductionYear = item.ProductionYear,
                Type = item.GetBaseItemKind().ToString(),
                CheckedUtc = marker
            });
        }

        return unmatched
            .OrderBy(i => i.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Queues a resolver run that re-queries items the negative cache would normally skip.
    /// </summary>
    /// <returns>A <see cref="NoContentResult"/> once the run is queued.</returns>
    [HttpPost("Rerun")]
    public ActionResult Rerun()
    {
        ResolveJustWatchLinksTask.RequestForceRun();
        _taskManager.QueueIfNotRunning<ResolveJustWatchLinksTask>();
        return NoContent();
    }
}
