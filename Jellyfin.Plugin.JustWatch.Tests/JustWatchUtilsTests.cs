using Jellyfin.Plugin.JustWatch;
using Xunit;

namespace Jellyfin.Plugin.JustWatch.Tests;

public class JustWatchUtilsTests
{
    [Fact]
    public void BuildUrl_FullPath_PrependsDomain()
    {
        Assert.Equal("https://www.justwatch.com/us/movie/the-matrix", JustWatchUtils.BuildUrl("/us/movie/the-matrix"));
    }

    [Fact]
    public void BuildUrl_SlugWithoutLeadingSlash_AddsSlash()
    {
        Assert.Equal("https://www.justwatch.com/us/movie/the-matrix", JustWatchUtils.BuildUrl("us/movie/the-matrix"));
    }

    [Fact]
    public void BuildUrl_AbsoluteHttpsUrl_PassesThrough()
    {
        Assert.Equal("https://www.justwatch.com/x", JustWatchUtils.BuildUrl("https://www.justwatch.com/x"));
    }

    [Fact]
    public void BuildUrl_AbsoluteHttpUrl_PassesThrough()
    {
        Assert.Equal("http://example.com/x", JustWatchUtils.BuildUrl("http://example.com/x"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void BuildUrl_EmptyOrWhitespace_ReturnsNull(string? id)
    {
        Assert.Null(JustWatchUtils.BuildUrl(id));
    }

    [Fact]
    public void ProviderName_IsJustWatch()
    {
        Assert.Equal("JustWatch", JustWatchUtils.ProviderName);
    }

    [Fact]
    public void BuildSeasonUrl_AppendsSeasonSegment()
    {
        Assert.Equal(
            "https://www.justwatch.com/us/tv-show/blakes-7/season-1",
            JustWatchUtils.BuildSeasonUrl("/us/tv-show/blakes-7", 1));
    }

    [Fact]
    public void BuildSeasonUrl_BareSlug_IsNormalized()
    {
        Assert.Equal(
            "https://www.justwatch.com/us/tv-show/x/season-2",
            JustWatchUtils.BuildSeasonUrl("us/tv-show/x", 2));
    }

    [Fact]
    public void BuildSeasonUrl_NullSeriesId_ReturnsNull()
    {
        Assert.Null(JustWatchUtils.BuildSeasonUrl(null, 1));
    }

    [Fact]
    public void BuildSeasonId_AppendsSeasonSegment()
    {
        Assert.Equal("/us/tv-show/blakes-7/season-3", JustWatchUtils.BuildSeasonId("/us/tv-show/blakes-7", 3));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void BuildSeasonId_EmptySeriesId_ReturnsNull(string? seriesId)
    {
        Assert.Null(JustWatchUtils.BuildSeasonId(seriesId, 1));
    }
}
