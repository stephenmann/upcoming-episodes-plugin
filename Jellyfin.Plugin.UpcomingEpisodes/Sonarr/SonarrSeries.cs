using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.UpcomingEpisodes.Sonarr;

/// <summary>
/// The series information returned with a Sonarr calendar entry.
/// </summary>
public class SonarrSeries
{
    /// <summary>
    /// Gets or sets the series title.
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the year the series first aired.
    /// </summary>
    [JsonPropertyName("year")]
    public int Year { get; set; }

    /// <summary>
    /// Gets or sets the TVDB id.
    /// </summary>
    [JsonPropertyName("tvdbId")]
    public int TvdbId { get; set; }

    /// <summary>
    /// Gets or sets the TMDB id.
    /// </summary>
    [JsonPropertyName("tmdbId")]
    public int TmdbId { get; set; }

    /// <summary>
    /// Gets or sets the IMDb id.
    /// </summary>
    [JsonPropertyName("imdbId")]
    public string? ImdbId { get; set; }
}
