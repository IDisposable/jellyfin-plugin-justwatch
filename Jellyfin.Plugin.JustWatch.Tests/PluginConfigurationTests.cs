using Jellyfin.Plugin.JustWatch.Configuration;
using Xunit;

namespace Jellyfin.Plugin.JustWatch.Tests;

public class PluginConfigurationTests
{
    [Fact]
    public void Defaults_AreSane()
    {
        var config = new PluginConfiguration();
        Assert.False(config.ResolveLinksEnabled);
        Assert.Equal("US", config.Country);
        Assert.Equal("en", config.Language);
        Assert.Equal(300, config.RequestDelayMs);
    }
}
