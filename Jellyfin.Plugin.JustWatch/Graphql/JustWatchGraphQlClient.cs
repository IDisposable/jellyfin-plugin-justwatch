using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.JustWatch.Graphql;

/// <summary>
/// Minimal client for JustWatch's <b>unofficial</b> GraphQL API (<c>https://apis.justwatch.com/graphql</c>).
/// Resolves a title's <c>fullPath</c> (e.g. <c>/us/movie/the-matrix</c>) by search, preferring an
/// external-id (TMDB/IMDb) match and falling back to title+year.
/// </summary>
/// <remarks>
/// The API is undocumented and may change without notice. TMDB ids are returned bare (e.g.
/// <c>"101"</c>), so id matching uses exact equality.
/// </remarks>
public sealed class JustWatchGraphQlClient
{
    private const string Endpoint = "https://apis.justwatch.com/graphql";

    private const string SearchQuery = @"
query SearchTitle($filter: TitleFilter!, $country: Country!, $language: Language!, $first: Int!) {
  popularTitles(country: $country, filter: $filter, first: $first, sortBy: POPULAR) {
    edges {
      node {
        __typename
        ... on MovieOrShow {
          objectType
          content(country: $country, language: $language) {
            title
            originalReleaseYear
            fullPath
            externalIds { imdbId tmdbId }
          }
        }
      }
    }
  }
}";

    private static readonly JsonSerializerOptions _writeOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly JsonSerializerOptions _readOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<JustWatchGraphQlClient> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="JustWatchGraphQlClient"/> class.
    /// </summary>
    /// <param name="httpClientFactory">The HTTP client factory.</param>
    /// <param name="logger">The logger.</param>
    public JustWatchGraphQlClient(IHttpClientFactory httpClientFactory, ILogger<JustWatchGraphQlClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Resolves a JustWatch <c>fullPath</c> for a title.
    /// </summary>
    /// <param name="title">The title to search for.</param>
    /// <param name="year">The release year, if known (used for fallback matching).</param>
    /// <param name="tmdbId">The TMDB id, if known (preferred match).</param>
    /// <param name="imdbId">The IMDb id, if known (preferred match).</param>
    /// <param name="objectType">JustWatch object type to constrain the search ("MOVIE" or "SHOW").</param>
    /// <param name="country">Country code (ISO 3166-1 alpha-2), e.g. "US".</param>
    /// <param name="language">Language code (ISO 639-1), e.g. "en".</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The matched <c>fullPath</c>, or <see langword="null"/> if none.</returns>
    public async Task<string?> ResolveFullPathAsync(
        string title,
        int? year,
        int? tmdbId,
        string? imdbId,
        string objectType,
        string country,
        string language,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return null;
        }

        var requestBody = new
        {
            operationName = "SearchTitle",
            query = SearchQuery,
            variables = new
            {
                filter = new { searchQuery = title, objectTypes = new[] { objectType } },
                country,
                language,
                first = 5
            }
        };

        string json;
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint)
            {
                Content = new StringContent(JsonSerializer.Serialize(requestBody, _writeOptions), Encoding.UTF8, "application/json")
            };

            // Headers the JustWatch web client sends.
            request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (compatible; Jellyfin.Plugin.JustWatch)");
            request.Headers.TryAddWithoutValidation("App-Version", "3.8.0-web-web");

            var client = _httpClientFactory.CreateClient(NamedClient.Default);
            using var httpResponse = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (!httpResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("JustWatch search for {Title} returned {Status}", title, httpResponse.StatusCode);
                return null;
            }

            json = await httpResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "JustWatch search for {Title} failed", title);
            return null;
        }

        try
        {
            return ResolveFromJson(json, tmdbId, imdbId, title, year);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "JustWatch response parse failed for {Title}", title);
            return null;
        }
    }

    /// <summary>
    /// Parses a raw JustWatch search response and selects the best <c>fullPath</c> match. Exposed for
    /// testing against captured sample responses.
    /// </summary>
    /// <param name="json">The raw GraphQL response JSON.</param>
    /// <param name="tmdbId">The TMDB id, if known (preferred match).</param>
    /// <param name="imdbId">The IMDb id, if known.</param>
    /// <param name="title">The title (fallback match).</param>
    /// <param name="year">The release year (fallback match).</param>
    /// <returns>The matched <c>fullPath</c>, or <see langword="null"/>.</returns>
    internal static string? ResolveFromJson(string json, int? tmdbId, string? imdbId, string? title, int? year)
        => SelectFullPath(JsonSerializer.Deserialize<GraphQlResponse>(json, _readOptions), tmdbId, imdbId, title, year);

    private static string? SelectFullPath(GraphQlResponse? response, int? tmdbId, string? imdbId, string? title, int? year)
    {
        var nodes = response?.Data?.PopularTitles?.Edges?
            .Select(e => e.Node?.Content)
            .Where(c => c is not null && !string.IsNullOrEmpty(c.FullPath))
            .Select(c => c!)
            .ToList();
        if (nodes is null || nodes.Count == 0)
        {
            return null;
        }

        // Prefer an exact external-id match (JustWatch returns bare ids, e.g. "101").
        if (tmdbId.HasValue)
        {
            var tmdbStr = tmdbId.Value.ToString(CultureInfo.InvariantCulture);
            var byTmdb = nodes.FirstOrDefault(c => string.Equals(c.ExternalIds?.TmdbId, tmdbStr, StringComparison.Ordinal));
            if (byTmdb is not null)
            {
                return byTmdb.FullPath;
            }
        }

        if (!string.IsNullOrEmpty(imdbId))
        {
            var byImdb = nodes.FirstOrDefault(c => string.Equals(c.ExternalIds?.ImdbId, imdbId, StringComparison.OrdinalIgnoreCase));
            if (byImdb is not null)
            {
                return byImdb.FullPath;
            }
        }

        // Fallback: title (+ year) match.
        var byTitle = nodes.FirstOrDefault(c =>
            string.Equals(c.Title, title, StringComparison.OrdinalIgnoreCase)
            && (!year.HasValue || c.OriginalReleaseYear == year.Value));
        return byTitle?.FullPath;
    }

    private sealed class GraphQlResponse
    {
        public GraphQlData? Data { get; set; }
    }

    private sealed class GraphQlData
    {
        public PopularTitlesConnection? PopularTitles { get; set; }
    }

    private sealed class PopularTitlesConnection
    {
        public List<TitleEdge>? Edges { get; set; }
    }

    private sealed class TitleEdge
    {
        public TitleNode? Node { get; set; }
    }

    private sealed class TitleNode
    {
        public string? ObjectType { get; set; }

        public TitleContent? Content { get; set; }
    }

    private sealed class TitleContent
    {
        public string? Title { get; set; }

        public int? OriginalReleaseYear { get; set; }

        public string? FullPath { get; set; }

        public TitleExternalIds? ExternalIds { get; set; }
    }

    private sealed class TitleExternalIds
    {
        public string? ImdbId { get; set; }

        public string? TmdbId { get; set; }
    }
}
