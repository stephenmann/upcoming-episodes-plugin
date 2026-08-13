using Jellyfin.Plugin.UpcomingEpisodes.Services;
using Jellyfin.Plugin.UpcomingEpisodes.Sonarr;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.UpcomingEpisodes;

/// <summary>
/// Registers the plugin services.
/// </summary>
public class PluginServiceRegistrator : IPluginServiceRegistrator
{
    /// <inheritdoc />
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        serviceCollection.AddSingleton<SonarrApiClient>();
        serviceCollection.AddSingleton<OverviewStateStore>();
        serviceCollection.AddSingleton<UpcomingEpisodesService>();
        serviceCollection.AddHostedService<ScheduleSynchronizer>();
    }
}
