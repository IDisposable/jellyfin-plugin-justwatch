using System;

namespace Jellyfin.Plugin.JustWatch.Api;

/// <summary>
/// One movie or series the resolver searched but couldn't match (negative-cached).
/// </summary>
public class JustWatchUnmatchedItem
{
    /// <summary>
    /// Gets or sets the item's library id (for linking to it in the web client).
    /// </summary>
    public Guid ItemId { get; set; }

    /// <summary>
    /// Gets or sets the item's name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the production year, if known.
    /// </summary>
    public int? ProductionYear { get; set; }

    /// <summary>
    /// Gets or sets the item kind ("Movie" or "Series").
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp (round-trip "O" format) of the last resolver miss.
    /// </summary>
    public string? CheckedUtc { get; set; }
}
