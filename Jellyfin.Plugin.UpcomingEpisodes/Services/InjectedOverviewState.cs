using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.UpcomingEpisodes.Services;

/// <summary>
/// Persisted record of a message that was written onto a series overview.
/// </summary>
public class InjectedOverviewState
{
    /// <summary>
    /// Gets or sets the overview the series had before a message was added.
    /// </summary>
    [JsonPropertyName("originalOverview")]
    public string OriginalOverview { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the message that was added.
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}
