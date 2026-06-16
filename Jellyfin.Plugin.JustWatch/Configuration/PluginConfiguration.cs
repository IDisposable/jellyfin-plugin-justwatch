using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.JustWatch.Configuration;

/// <summary>
/// JustWatch plugin configuration.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginConfiguration"/> class.
    /// </summary>
    public PluginConfiguration()
    {
        Country = "US";
        Language = "en";
        ResolveLinksEnabled = false;
        RequestDelayMs = 300;
    }

    /// <summary>
    /// Gets or sets a value indicating whether the "Resolve JustWatch links" task may query the
    /// (unofficial) JustWatch GraphQL API to auto-populate the JustWatch id on library items.
    /// Off by default; uses an unofficial endpoint.
    /// </summary>
    public bool ResolveLinksEnabled { get; set; }

    /// <summary>
    /// Gets or sets the country code (ISO 3166-1 alpha-2) used for JustWatch lookups/offers.
    /// </summary>
    public string Country { get; set; }

    /// <summary>
    /// Gets or sets the language (ISO 639-1) used for JustWatch lookups.
    /// </summary>
    public string Language { get; set; }

    /// <summary>
    /// Gets or sets the delay, in milliseconds, between resolver requests to the unofficial JustWatch
    /// endpoint. Throttles the opt-in task to avoid rate-limiting; clamped to non-negative.
    /// </summary>
    public int RequestDelayMs { get; set; }
}
