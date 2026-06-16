using System;
using System.Collections.Generic;
using System.Globalization;
using Jellyfin.Plugin.JustWatch.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.JustWatch;

/// <summary>
/// JustWatch integration: contributes a JustWatch external id and a clickable deeplink to
/// movies, series, seasons, and episodes. The unofficial JustWatch API risk is isolated to this
/// plugin; other plugins integrate by reading <c>ProviderIds["JustWatch"]</c>.
/// </summary>
public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Plugin"/> class.
    /// </summary>
    /// <param name="applicationPaths">Instance of the <see cref="IApplicationPaths"/> interface.</param>
    /// <param name="xmlSerializer">Instance of the <see cref="IXmlSerializer"/> interface.</param>
    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
    }

    /// <summary>
    /// Gets the current plugin instance.
    /// </summary>
    public static Plugin? Instance { get; private set; }

    /// <inheritdoc />
    public override string Name => "JustWatch";

    /// <inheritdoc />
    public override string Description =>
        "Adds JustWatch external IDs and deep-links to movies, series, seasons, and episodes.";

    /// <inheritdoc />
    public override Guid Id => Guid.Parse("f8bb9df9-54fb-48cf-babb-212db1116f48");

    /// <inheritdoc />
    public IEnumerable<PluginPageInfo> GetPages()
    {
        yield return new PluginPageInfo
        {
            Name = "JustWatch",
            EmbeddedResourcePath = string.Format(CultureInfo.InvariantCulture, "{0}.Configuration.configPage.html", GetType().Namespace)
        };
    }
}
