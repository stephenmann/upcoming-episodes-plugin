using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.UpcomingEpisodes.Configuration;

/// <summary>
/// Plugin configuration.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Gets or sets the base URL of the Sonarr server, e.g. http://localhost:8989.
    /// </summary>
    public string SonarrUrl { get; set; } = "http://localhost:8989";

    /// <summary>
    /// Gets or sets the Sonarr API key.
    /// </summary>
    public string SonarrApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of days ahead to query the Sonarr calendar.
    /// </summary>
    public int LookaheadDays { get; set; } = 30;

    /// <summary>
    /// Gets or sets the hour of the day (0-23, local time) the nightly refresh runs.
    /// </summary>
    public int ScheduleHour { get; set; } = 3;

    /// <summary>
    /// Gets or sets the minute of the hour (0-59) the nightly refresh runs.
    /// </summary>
    public int ScheduleMinute { get; set; }

    /// <summary>
    /// Gets or sets the first day of the week used to decide whether an air date falls in the current week.
    /// </summary>
    public DayOfWeek FirstDayOfWeek { get; set; } = DayOfWeek.Sunday;

    /// <summary>
    /// Gets or sets a value indicating whether unmonitored episodes are included.
    /// </summary>
    public bool IncludeUnmonitored { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether episodes that already have a file are included.
    /// </summary>
    public bool IncludeDownloaded { get; set; }
}
