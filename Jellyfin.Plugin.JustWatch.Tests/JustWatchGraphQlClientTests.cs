using System.Text.Json;
using Jellyfin.Plugin.JustWatch.Graphql;
using Xunit;

namespace Jellyfin.Plugin.JustWatch.Tests;

public class JustWatchGraphQlClientTests
{
    [Fact]
    public void ResolveFromJson_Movie_ByTmdbId()
    {
        Assert.Equal(
            "/us/movie/leon-the-professional",
            JustWatchGraphQlClient.ResolveFromJson(SampleResponses.MovieSearch, 101, null, null, null));
    }

    [Fact]
    public void ResolveFromJson_Movie_ByImdbId()
    {
        Assert.Equal(
            "/us/movie/leon-the-professional",
            JustWatchGraphQlClient.ResolveFromJson(SampleResponses.MovieSearch, null, "tt0110413", null, null));
    }

    [Fact]
    public void ResolveFromJson_TmdbMatch_IsExact_NotSubstring()
    {
        // "10" must NOT match the real id "101" (regression guard for exact-equality matching).
        Assert.Null(JustWatchGraphQlClient.ResolveFromJson(SampleResponses.MovieSearch, 10, null, null, null));
    }

    [Fact]
    public void ResolveFromJson_Show_ByTmdbId()
    {
        Assert.Equal(
            "/us/tv-show/blakes-7",
            JustWatchGraphQlClient.ResolveFromJson(SampleResponses.ShowSearch, 1731, null, null, null));
    }

    [Fact]
    public void ResolveFromJson_TitleYearFallback_PicksCorrectEntry()
    {
        // No ids: fall back to title and year. "The Professional" (1981) is the second movie.
        Assert.Equal(
            "/us/movie/the-professional-1981",
            JustWatchGraphQlClient.ResolveFromJson(SampleResponses.MovieSearch, null, null, "The Professional", 1981));
    }

    [Fact]
    public void ResolveFromJson_NoMatch_ReturnsNull()
    {
        Assert.Null(JustWatchGraphQlClient.ResolveFromJson(SampleResponses.MovieSearch, 999999, null, null, null));
    }

    [Fact]
    public void ResolveFromJson_EmptyResults_ReturnsNull()
    {
        Assert.Null(JustWatchGraphQlClient.ResolveFromJson(SampleResponses.Empty, 101, null, null, null));
    }

    [Fact]
    public void ResolveFromJson_TmdbPreferredOverImdb_WhenBothGivenAndDiffer()
    {
        // tmdb 1672 = "The Professional" (1981); imdb tt0110413 = Léon. TMDB is checked first and wins.
        Assert.Equal(
            "/us/movie/the-professional-1981",
            JustWatchGraphQlClient.ResolveFromJson(SampleResponses.MovieSearch, 1672, "tt0110413", null, null));
    }

    [Fact]
    public void ResolveFromJson_ImdbMatch_IsCaseInsensitive()
    {
        Assert.Equal(
            "/us/movie/leon-the-professional",
            JustWatchGraphQlClient.ResolveFromJson(SampleResponses.MovieSearch, null, "TT0110413", null, null));
    }

    [Fact]
    public void ResolveFromJson_TitleFallback_IsCaseInsensitive()
    {
        Assert.Equal(
            "/us/tv-show/blakes-7",
            JustWatchGraphQlClient.ResolveFromJson(SampleResponses.ShowSearch, null, null, "blake's 7", null));
    }

    [Fact]
    public void ResolveFromJson_TitleFallback_NullYear_MatchesOnTitleAlone()
    {
        Assert.Equal(
            "/us/movie/the-professional-1981",
            JustWatchGraphQlClient.ResolveFromJson(SampleResponses.MovieSearch, null, null, "The Professional", null));
    }

    [Fact]
    public void ResolveFromJson_TitleFallback_WrongYear_ReturnsNull()
    {
        Assert.Null(
            JustWatchGraphQlClient.ResolveFromJson(SampleResponses.MovieSearch, null, null, "The Professional", 1999));
    }

    [Fact]
    public void ResolveFromJson_MalformedJson_Throws()
    {
        // Contract: ResolveFullPathAsync relies on this throwing so it can swallow it and return null.
        Assert.Throws<JsonException>(
            () => JustWatchGraphQlClient.ResolveFromJson("not json", 101, null, null, null));
    }

    [Fact]
    public void ResolveFromJson_NullData_ReturnsNull()
    {
        Assert.Null(JustWatchGraphQlClient.ResolveFromJson("{\"data\":null}", 101, null, null, null));
    }

    [Fact]
    public void ResolveFromJson_MissingPopularTitles_ReturnsNull()
    {
        Assert.Null(JustWatchGraphQlClient.ResolveFromJson("{\"data\":{\"popularTitles\":null}}", 101, null, null, null));
    }
}
