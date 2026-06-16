using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.JustWatch;

/// <summary>
/// Base JustWatch external id. Stores a JustWatch <c>fullPath</c> in <c>ProviderIds["JustWatch"]</c>.
/// </summary>
public abstract class JustWatchExternalId : IExternalId
{
    /// <inheritdoc />
    public string ProviderName => JustWatchUtils.ProviderName;

    /// <inheritdoc />
    public string Key => JustWatchUtils.ProviderName;

    /// <inheritdoc />
    public abstract ExternalIdMediaType? Type { get; }

    /// <inheritdoc />
    public abstract bool Supports(IHasProviderIds item);
}
