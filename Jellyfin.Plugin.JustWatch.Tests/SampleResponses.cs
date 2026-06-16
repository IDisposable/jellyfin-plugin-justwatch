namespace Jellyfin.Plugin.JustWatch.Tests;

/// <summary>
/// Real JustWatch GraphQL search responses, captured verbatim from the live endpoint
/// (<c>POST https://apis.justwatch.com/graphql</c>) and used as parser fixtures. Refresh these if
/// JustWatch changes its schema.
/// </summary>
internal static class SampleResponses
{
    /// <summary>
    /// Search for "Leon The Professional". Top hit is the movie; the list also has a second movie and
    /// a show, which exercises matching against mixed results.
    /// </summary>
    public const string MovieSearch =
        """
        {"data":{"popularTitles":{"edges":[{"node":{"__typename":"Movie","objectType":"MOVIE","content":{"title":"Léon: The Professional","originalReleaseYear":1994,"fullPath":"/us/movie/leon-the-professional","externalIds":{"imdbId":"tt0110413","tmdbId":"101"}}}},{"node":{"__typename":"Movie","objectType":"MOVIE","content":{"title":"The Professional","originalReleaseYear":1981,"fullPath":"/us/movie/the-professional-1981","externalIds":{"imdbId":"tt0082949","tmdbId":"1672"}}}},{"node":{"__typename":"Show","objectType":"SHOW","content":{"title":"Dr. Katz, Professional Therapist","originalReleaseYear":1995,"fullPath":"/us/tv-show/dr-katz-professional-therapist","externalIds":{"imdbId":"tt0111942","tmdbId":"2513"}}}}]}}}
        """;

    /// <summary>
    /// Search for "Blake's 7". Top hit is the show; the other hits are unrelated movies with no
    /// same-named film.
    /// </summary>
    public const string ShowSearch =
        """
        {"data":{"popularTitles":{"edges":[{"node":{"__typename":"Show","objectType":"SHOW","content":{"title":"Blake's 7","originalReleaseYear":1978,"fullPath":"/us/tv-show/blakes-7","externalIds":{"imdbId":"tt2412916","tmdbId":"1731"}}}},{"node":{"__typename":"Movie","objectType":"MOVIE","content":{"title":"Scream 7","originalReleaseYear":2026,"fullPath":"/us/movie/scream-7","externalIds":{"imdbId":"tt27047903","tmdbId":"1159559"}}}},{"node":{"__typename":"Movie","objectType":"MOVIE","content":{"title":"Furious 7","originalReleaseYear":2015,"fullPath":"/us/movie/fast-7","externalIds":{"imdbId":"tt2820852","tmdbId":"168259"}}}}]}}}
        """;

    /// <summary>
    /// A well-formed response with no results.
    /// </summary>
    public const string Empty =
        """
        {"data":{"popularTitles":{"edges":[]}}}
        """;
}
