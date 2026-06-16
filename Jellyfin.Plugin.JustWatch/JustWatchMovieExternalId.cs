using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.JustWatch;

/// <summary>
/// JustWatch external id for movies (distinct movie pages, e.g. /us/movie/x).
/// </summary>
public class JustWatchMovieExternalId : JustWatchExternalId
{
    /// <inheritdoc />
    public override ExternalIdMediaType? Type => ExternalIdMediaType.Movie;

    /// <inheritdoc />
    public override bool Supports(IHasProviderIds item) => item is Movie;
}
