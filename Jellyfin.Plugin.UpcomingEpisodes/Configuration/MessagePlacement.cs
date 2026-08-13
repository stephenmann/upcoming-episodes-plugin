namespace Jellyfin.Plugin.UpcomingEpisodes.Configuration;

/// <summary>
/// Where the upcoming episode message is shown.
/// </summary>
public enum MessagePlacement
{
    /// <summary>
    /// Next to the star rating when the File Transformation plugin is available, otherwise in the overview.
    /// </summary>
    Automatic = 0,

    /// <summary>
    /// Prepended to the series overview.
    /// </summary>
    SeriesOverview = 1,

    /// <summary>
    /// Shown next to the star rating. Requires the File Transformation plugin.
    /// </summary>
    NextToRating = 2,

    /// <summary>
    /// Shown in both places.
    /// </summary>
    Both = 3
}
