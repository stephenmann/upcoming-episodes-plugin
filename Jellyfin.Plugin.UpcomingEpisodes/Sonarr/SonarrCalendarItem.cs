using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.UpcomingEpisodes.Sonarr;

/// <summary>
/// A single entry of the Sonarr calendar (an episode).
/// </summary>
public class SonarrCalendarItem
{
    /// <summary>
    /// Gets or sets the Sonarr series id.
    /// </summary>
    [JsonPropertyName("seriesId")]
    public int SeriesId { get; set; }

    /// <summary>
    /// Gets or sets the season number.
    /// </summary>
    [JsonPropertyName("seasonNumber")]
    public int SeasonNumber { get; set; }

    /// <summary>
    /// Gets or sets the episode number.
    /// </summary>
    [JsonPropertyName("episodeNumber")]
    public int EpisodeNumber { get; set; }

    /// <summary>
    /// Gets or sets the episode title.
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the local air date in yyyy-MM-dd form.
    /// </summary>
    [JsonPropertyName("airDate")]
    public string? AirDate { get; set; }

    /// <summary>
    /// Gets or sets the air date in UTC.
    /// </summary>
    [JsonPropertyName("airDateUtc")]
    public DateTime? AirDateUtc { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the episode is monitored.
    /// </summary>
    [JsonPropertyName("monitored")]
    public bool Monitored { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the episode file already exists.
    /// </summary>
    [JsonPropertyName("hasFile")]
    public bool HasFile { get; set; }

    /// <summary>
    /// Gets or sets the series the episode belongs to.
    /// </summary>
    [JsonPropertyName("series")]
    public SonarrSeries? Series { get; set; }
}
