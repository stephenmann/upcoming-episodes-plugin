using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using Jellyfin.Plugin.UpcomingEpisodes.Configuration;
using MediaBrowser.Common.Net;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.UpcomingEpisodes.Sonarr;

/// <summary>
/// Minimal Sonarr API client covering the calendar endpoint.
/// </summary>
public class SonarrApiClient
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SonarrApiClient> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SonarrApiClient"/> class.
    /// </summary>
    /// <param name="httpClientFactory">Instance of the <see cref="IHttpClientFactory"/> interface.</param>
    /// <param name="logger">Instance of the <see cref="ILogger{SonarrApiClient}"/> interface.</param>
    public SonarrApiClient(IHttpClientFactory httpClientFactory, ILogger<SonarrApiClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Gets the Sonarr calendar between two dates.
    /// </summary>
    /// <param name="configuration">The plugin configuration.</param>
    /// <param name="start">Inclusive start date.</param>
    /// <param name="end">Exclusive end date.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The calendar entries.</returns>
    public async Task<IReadOnlyList<SonarrCalendarItem>> GetCalendarAsync(
        PluginConfiguration configuration,
        DateTime start,
        DateTime end,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(configuration.SonarrUrl) || string.IsNullOrWhiteSpace(configuration.SonarrApiKey))
        {
            throw new InvalidOperationException("The Sonarr URL and API key must be configured.");
        }

        var requestUri = string.Format(
            CultureInfo.InvariantCulture,
            "{0}/api/v3/calendar?start={1}&end={2}&unmonitored={3}&includeSeries=true",
            configuration.SonarrUrl.TrimEnd('/'),
            Uri.EscapeDataString(start.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
            Uri.EscapeDataString(end.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
            configuration.IncludeUnmonitored ? "true" : "false");

        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.Add("X-Api-Key", configuration.SonarrApiKey);

        var httpClient = _httpClientFactory.CreateClient(NamedClient.Default);
        using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var items = await response.Content
            .ReadFromJsonAsync<List<SonarrCalendarItem>>(_jsonOptions, cancellationToken)
            .ConfigureAwait(false);

        _logger.LogDebug("Sonarr returned {Count} calendar entries.", items?.Count ?? 0);
        return items ?? new List<SonarrCalendarItem>();
    }
}
