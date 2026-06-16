namespace Jellyfin.Plugin.JustWatch.Api;

/// <summary>
/// Resolver coverage counts across the movies and series the task scans.
/// </summary>
public class JustWatchCounts
{
    /// <summary>
    /// Gets or sets the number of items that already have a JustWatch id.
    /// </summary>
    public int Matched { get; set; }

    /// <summary>
    /// Gets or sets the number of items that were searched but didn't resolve (negative-cached).
    /// </summary>
    public int Unmatched { get; set; }

    /// <summary>
    /// Gets or sets the number of items not yet searched.
    /// </summary>
    public int Unqueried { get; set; }

    /// <summary>
    /// Gets or sets the total number of movies and series scanned.
    /// </summary>
    public int Total { get; set; }
}
