using Jellyfin.Plugin.JustWatch;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Providers;
using Xunit;

namespace Jellyfin.Plugin.JustWatch.Tests;

public class JustWatchExternalIdTests
{
    [Fact]
    public void Movie_SupportsOnlyMovie()
    {
        var id = new JustWatchMovieExternalId();
        Assert.Equal(ExternalIdMediaType.Movie, id.Type);
        Assert.Equal("JustWatch", id.Key);
        Assert.Equal("JustWatch", id.ProviderName);
        Assert.True(id.Supports(new Movie()));
        Assert.False(id.Supports(new Series()));
        Assert.False(id.Supports(new Season()));
        Assert.False(id.Supports(new Episode()));
    }

    [Fact]
    public void Series_SupportsOnlySeries()
    {
        var id = new JustWatchSeriesExternalId();
        Assert.Equal(ExternalIdMediaType.Series, id.Type);
        Assert.True(id.Supports(new Series()));
        Assert.False(id.Supports(new Movie()));
        Assert.False(id.Supports(new Season()));
        Assert.False(id.Supports(new Episode()));
    }

    [Fact]
    public void Season_SupportsOnlySeason()
    {
        var id = new JustWatchSeasonExternalId();
        Assert.Equal(ExternalIdMediaType.Season, id.Type);
        Assert.True(id.Supports(new Season()));
        Assert.False(id.Supports(new Movie()));
        Assert.False(id.Supports(new Series()));
        Assert.False(id.Supports(new Episode()));
    }

    [Fact]
    public void Episode_SupportsOnlyEpisode()
    {
        var id = new JustWatchEpisodeExternalId();
        Assert.Equal(ExternalIdMediaType.Episode, id.Type);
        Assert.True(id.Supports(new Episode()));
        Assert.False(id.Supports(new Movie()));
        Assert.False(id.Supports(new Series()));
        Assert.False(id.Supports(new Season()));
    }
}
