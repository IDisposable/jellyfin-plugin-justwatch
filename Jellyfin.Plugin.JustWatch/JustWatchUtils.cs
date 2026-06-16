using System;
using System.Globalization;

namespace Jellyfin.Plugin.JustWatch;

/// <summary>
/// Shared constants and helpers for JustWatch integration.
/// </summary>
public static class JustWatchUtils
{
    /// <summary>
    /// The provider/display name and the key used in <c>ProviderIds</c>.
    /// </summary>
    public const string ProviderName = "JustWatch";

    /// <summary>
    /// <c>ProviderIds</c> key holding the UTC timestamp (round-trip "O" format) of the last resolver
    /// miss. No <see cref="MediaBrowser.Controller.Providers.IExternalId"/> is registered for it, so it
    /// stays out of the metadata editor; it's just a negative cache.
    /// </summary>
    public const string CheckedProviderName = "JustWatchCheckedUtc";

    /// <summary>
    /// The public JustWatch site base URL (no trailing slash).
    /// </summary>
    public const string BaseUrl = "https://www.justwatch.com";

    /// <summary>
    /// The internal GraphQL endpoint consumed by the JustWatch web/app clients.
    /// </summary>
    public const string GraphQlEndpoint = "https://apis.justwatch.com/graphql";

    /// <summary>
    /// Builds a public JustWatch URL from a stored provider id.
    /// </summary>
    /// <remarks>
    /// The stored id is expected to be a JustWatch <c>fullPath</c> (e.g. <c>/us/movie/the-matrix</c>).
    /// Absolute URLs and bare slugs are tolerated.
    /// </remarks>
    /// <param name="id">The stored JustWatch provider id.</param>
    /// <returns>An absolute JustWatch URL, or <see langword="null"/> if the id is empty.</returns>
    public static string? BuildUrl(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        if (id.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || id.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return id;
        }

        return id[0] == '/' ? BaseUrl + id : BaseUrl + "/" + id;
    }

    /// <summary>
    /// Builds a JustWatch season <c>fullPath</c> (id form) from a series id and season number. For
    /// example, "/us/tv-show/blakes-7" with season 1 gives "/us/tv-show/blakes-7/season-1".
    /// </summary>
    /// <param name="seriesId">The series' JustWatch id (fullPath).</param>
    /// <param name="seasonNumber">The season number.</param>
    /// <returns>The season <c>fullPath</c>, or <see langword="null"/> if the series id is empty.</returns>
    public static string? BuildSeasonId(string? seriesId, int seasonNumber)
        => string.IsNullOrWhiteSpace(seriesId)
            ? null
            : string.Create(CultureInfo.InvariantCulture, $"{seriesId}/season-{seasonNumber}");

    /// <summary>
    /// Builds a JustWatch season URL from a series id and season number. For example,
    /// "/us/tv-show/blakes-7" with season 1 gives "/us/tv-show/blakes-7/season-1".
    /// </summary>
    /// <param name="seriesId">The series' JustWatch id (fullPath).</param>
    /// <param name="seasonNumber">The season number.</param>
    /// <returns>An absolute JustWatch season URL, or <see langword="null"/> if the series id is empty.</returns>
    public static string? BuildSeasonUrl(string? seriesId, int seasonNumber)
        => BuildUrl(BuildSeasonId(seriesId, seasonNumber));

    /// <summary>
    /// Decides whether an unmatched item is still within its negative-cache window and should be
    /// skipped this run, based on the stored <see cref="CheckedProviderName"/> marker.
    /// </summary>
    /// <param name="checkedUtcMarker">The stored marker (round-trip UTC timestamp), or null/empty.</param>
    /// <param name="recheckUnmatchedDays">Days before re-checking a miss; 0 or less means never re-check.</param>
    /// <param name="utcNow">The current UTC time.</param>
    /// <returns><see langword="true"/> to skip the item (miss still cached); otherwise re-attempt it.</returns>
    public static bool ShouldSkipUnmatched(string? checkedUtcMarker, int recheckUnmatchedDays, DateTime utcNow)
    {
        if (string.IsNullOrEmpty(checkedUtcMarker)
            || !DateTime.TryParse(checkedUtcMarker, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var lastChecked))
        {
            return false;
        }

        return recheckUnmatchedDays <= 0
            || utcNow < lastChecked.ToUniversalTime().AddDays(recheckUnmatchedDays);
    }
}
