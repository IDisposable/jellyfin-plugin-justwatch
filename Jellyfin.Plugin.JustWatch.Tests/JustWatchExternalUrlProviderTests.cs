using System.Linq;
using Jellyfin.Plugin.JustWatch;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Entities;
using Xunit;

namespace Jellyfin.Plugin.JustWatch.Tests;

public class JustWatchExternalUrlProviderTests
{
    [Fact]
    public void Name_IsJustWatch()
    {
        Assert.Equal("JustWatch", new JustWatchExternalUrlProvider().Name);
    }

    [Fact]
    public void GetExternalUrls_WithId_YieldsDeepLink()
    {
        var movie = new Movie();
        movie.SetProviderId("JustWatch", "/us/movie/the-matrix");

        var urls = new JustWatchExternalUrlProvider().GetExternalUrls(movie).ToList();

        Assert.Equal("https://www.justwatch.com/us/movie/the-matrix", Assert.Single(urls));
    }

    [Fact]
    public void GetExternalUrls_WithoutId_IsEmpty()
    {
        var urls = new JustWatchExternalUrlProvider().GetExternalUrls(new Movie()).ToList();
        Assert.Empty(urls);
    }

    [Fact]
    public void GetExternalUrls_SeasonWithOwnId_YieldsOwnDeepLink()
    {
        var season = new Season();
        season.SetProviderId("JustWatch", "/us/tv-show/blakes-7/season-1");

        var urls = new JustWatchExternalUrlProvider().GetExternalUrls(season).ToList();

        // An explicit id takes precedence over any derived-from-series link.
        Assert.Equal("https://www.justwatch.com/us/tv-show/blakes-7/season-1", Assert.Single(urls));
    }

    [Fact]
    public void GetExternalUrls_EpisodeWithOwnId_YieldsOwnDeepLink()
    {
        var episode = new Episode();
        episode.SetProviderId("JustWatch", "/us/tv-show/blakes-7/season-1");

        var urls = new JustWatchExternalUrlProvider().GetExternalUrls(episode).ToList();

        Assert.Equal("https://www.justwatch.com/us/tv-show/blakes-7/season-1", Assert.Single(urls));
    }

    [Fact]
    public void GetExternalUrls_SeasonWithoutIdAndNoResolvableSeries_IsEmpty()
    {
        var urls = new JustWatchExternalUrlProvider().GetExternalUrls(new Season()).ToList();
        Assert.Empty(urls);
    }

    [Fact]
    public void GetExternalUrls_EpisodeWithoutIdAndNoResolvableSeries_IsEmpty()
    {
        var urls = new JustWatchExternalUrlProvider().GetExternalUrls(new Episode()).ToList();
        Assert.Empty(urls);
    }
}
