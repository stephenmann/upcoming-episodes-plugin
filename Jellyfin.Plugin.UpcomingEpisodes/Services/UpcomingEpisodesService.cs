using System.Globalization;
using System.Text;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.UpcomingEpisodes.Sonarr;
using Jellyfin.Plugin.UpcomingEpisodes.Web;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.UpcomingEpisodes.Services;

/// <summary>
/// Applies "next episode" messages from the Sonarr calendar onto Jellyfin series.
/// </summary>
public class UpcomingEpisodesService
{
    private const int MaxLookaheadDays = 30;

    private readonly ILibraryManager _libraryManager;
    private readonly SonarrApiClient _sonarrApiClient;
    private readonly OverviewStateStore _stateStore;
    private readonly UpcomingMessageStore _messageStore;
    private readonly FileTransformationRegistrar _fileTransformation;
    private readonly ILogger<UpcomingEpisodesService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpcomingEpisodesService"/> class.
    /// </summary>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    /// <param name="sonarrApiClient">The Sonarr API client.</param>
    /// <param name="stateStore">The overview state store.</param>
    /// <param name="messageStore">The message store read by the web client.</param>
    /// <param name="fileTransformation">The File Transformation registrar.</param>
    /// <param name="logger">Instance of the <see cref="ILogger{UpcomingEpisodesService}"/> interface.</param>
    public UpcomingEpisodesService(
        ILibraryManager libraryManager,
        SonarrApiClient sonarrApiClient,
        OverviewStateStore stateStore,
        UpcomingMessageStore messageStore,
        FileTransformationRegistrar fileTransformation,
        ILogger<UpcomingEpisodesService> logger)
    {
        _libraryManager = libraryManager;
        _sonarrApiClient = sonarrApiClient;
        _stateStore = stateStore;
        _messageStore = messageStore;
        _fileTransformation = fileTransformation;
        _logger = logger;
    }

    /// <summary>
    /// Queries Sonarr and updates every matching series in the library.
    /// </summary>
    /// <param name="progress">Progress reporter.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the refresh.</returns>
    public async Task RefreshAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        var configuration = Plugin.Instance?.Configuration
            ?? throw new InvalidOperationException("The plugin is not initialized.");

        var today = DateTime.Now.Date;
        var lookaheadDays = Math.Clamp(configuration.LookaheadDays, 1, MaxLookaheadDays);
        var calendar = await _sonarrApiClient
            .GetCalendarAsync(configuration, today, today.AddDays(lookaheadDays), cancellationToken)
            .ConfigureAwait(false);
        progress.Report(20);

        var nextEpisodes = GetNextEpisodePerSeries(calendar, configuration.IncludeDownloaded, today);
        _logger.LogInformation("Found upcoming episodes for {Count} Sonarr series.", nextEpisodes.Count);

        var seriesInLibrary = _libraryManager.GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = new[] { BaseItemKind.Series },
            Recursive = true,
            IsVirtualItem = false
        }).OfType<Series>().ToList();
        progress.Report(30);

        var state = _stateStore.Load();
        var messages = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var updatedItemIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var useOverview = !_fileTransformation.IsRegistered;
        var processed = 0;

        foreach (var (item, entry) in MatchSeries(seriesInLibrary, nextEpisodes))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var airDate = GetLocalAirDate(entry)!.Value;
            var message = UpcomingEpisodeMessageBuilder.Build(
                airDate,
                entry.EpisodeNumber,
                today,
                configuration.FirstDayOfWeek);

            var itemId = item.Id.ToString("N", CultureInfo.InvariantCulture);
            messages[itemId] = message;

            if (useOverview)
            {
                await ApplyMessageAsync(item, message, state, cancellationToken).ConfigureAwait(false);
                updatedItemIds.Add(itemId);
            }

            processed++;
            progress.Report(30 + (60d * processed / Math.Max(nextEpisodes.Count, 1)));
        }

        // Overviews touched by an earlier run are restored when the message moved to the web client.
        await ClearStaleMessagesAsync(state, updatedItemIds, cancellationToken).ConfigureAwait(false);

        _stateStore.Save(state);
        _messageStore.Replace(messages);
        progress.Report(100);
        _logger.LogInformation(
            "Upcoming episode messages set for {Count} series, shown {Placement}.",
            messages.Count,
            useOverview ? "in the series overview" : "next to the star rating");
    }

    private static Dictionary<string, SonarrCalendarItem> GetNextEpisodePerSeries(
        IReadOnlyList<SonarrCalendarItem> calendar,
        bool includeDownloaded,
        DateTime today)
    {
        var result = new Dictionary<string, SonarrCalendarItem>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in calendar)
        {
            if (entry.Series is null || (entry.HasFile && !includeDownloaded))
            {
                continue;
            }

            var airDate = GetLocalAirDate(entry);
            if (airDate is null || airDate.Value.Date < today)
            {
                continue;
            }

            var key = entry.SeriesId.ToString(CultureInfo.InvariantCulture);
            if (!result.TryGetValue(key, out var current)
                || GetLocalAirDate(current) > airDate
                || (GetLocalAirDate(current) == airDate && current.EpisodeNumber > entry.EpisodeNumber))
            {
                result[key] = entry;
            }
        }

        return result;
    }

    private static DateTime? GetLocalAirDate(SonarrCalendarItem entry)
    {
        if (entry.AirDateUtc.HasValue)
        {
            return DateTime.SpecifyKind(entry.AirDateUtc.Value, DateTimeKind.Utc).ToLocalTime().Date;
        }

        if (DateTime.TryParse(
                entry.AirDate,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsed))
        {
            return parsed.Date;
        }

        return null;
    }

    private static string NormalizeTitle(string? title, int year)
    {
        var builder = new StringBuilder();
        foreach (var character in title ?? string.Empty)
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToLowerInvariant(character));
            }
        }

        builder.Append('|').Append(year.ToString(CultureInfo.InvariantCulture));
        return builder.ToString();
    }

    private static string ComposeOverview(string message, string originalOverview)
    {
        return string.IsNullOrWhiteSpace(originalOverview)
            ? message
            : string.Concat(message, "\n\n", originalOverview);
    }

    private IEnumerable<(Series Item, SonarrCalendarItem Entry)> MatchSeries(
        IReadOnlyList<Series> seriesInLibrary,
        Dictionary<string, SonarrCalendarItem> nextEpisodes)
    {
        var byTvdb = new Dictionary<string, Series>(StringComparer.OrdinalIgnoreCase);
        var byImdb = new Dictionary<string, Series>(StringComparer.OrdinalIgnoreCase);
        var byTmdb = new Dictionary<string, Series>(StringComparer.OrdinalIgnoreCase);
        var byTitle = new Dictionary<string, Series>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in seriesInLibrary)
        {
            AddIfPresent(byTvdb, item.GetProviderId(MetadataProvider.Tvdb), item);
            AddIfPresent(byImdb, item.GetProviderId(MetadataProvider.Imdb), item);
            AddIfPresent(byTmdb, item.GetProviderId(MetadataProvider.Tmdb), item);
            AddIfPresent(byTitle, NormalizeTitle(item.Name, item.ProductionYear ?? 0), item);
        }

        foreach (var entry in nextEpisodes.Values)
        {
            var series = entry.Series!;
            var match = Lookup(byTvdb, series.TvdbId > 0 ? series.TvdbId.ToString(CultureInfo.InvariantCulture) : null)
                        ?? Lookup(byImdb, series.ImdbId)
                        ?? Lookup(byTmdb, series.TmdbId > 0 ? series.TmdbId.ToString(CultureInfo.InvariantCulture) : null)
                        ?? Lookup(byTitle, NormalizeTitle(series.Title, series.Year));

            if (match is null)
            {
                _logger.LogDebug("No library match for Sonarr series {Title} ({Year}).", series.Title, series.Year);
                continue;
            }

            yield return (match, entry);
        }

        static void AddIfPresent(Dictionary<string, Series> map, string? key, Series item)
        {
            if (!string.IsNullOrEmpty(key))
            {
                map.TryAdd(key, item);
            }
        }

        static Series? Lookup(Dictionary<string, Series> map, string? key)
        {
            return !string.IsNullOrEmpty(key) && map.TryGetValue(key, out var item) ? item : null;
        }
    }

    private async Task ApplyMessageAsync(
        BaseItem item,
        string message,
        Dictionary<string, InjectedOverviewState> state,
        CancellationToken cancellationToken)
    {
        var itemId = item.Id.ToString("N", CultureInfo.InvariantCulture);
        var currentOverview = item.Overview ?? string.Empty;
        var originalOverview = GetOriginalOverview(state, itemId, currentOverview);
        var newOverview = ComposeOverview(message, originalOverview);

        state[itemId] = new InjectedOverviewState
        {
            OriginalOverview = originalOverview,
            Message = message
        };

        if (string.Equals(currentOverview, newOverview, StringComparison.Ordinal))
        {
            return;
        }

        item.Overview = newOverview;
        await item.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, cancellationToken).ConfigureAwait(false);
        _logger.LogDebug("Set \"{Message}\" on {Name}.", message, item.Name);
    }

    private async Task ClearStaleMessagesAsync(
        Dictionary<string, InjectedOverviewState> state,
        HashSet<string> updatedItemIds,
        CancellationToken cancellationToken)
    {
        foreach (var itemId in state.Keys.Where(id => !updatedItemIds.Contains(id)).ToList())
        {
            cancellationToken.ThrowIfCancellationRequested();

            var previous = state[itemId];
            state.Remove(itemId);

            if (!Guid.TryParseExact(itemId, "N", out var guid))
            {
                continue;
            }

            var item = _libraryManager.GetItemById(guid);
            if (item is null)
            {
                continue;
            }

            var expected = ComposeOverview(previous.Message, previous.OriginalOverview);
            if (!string.Equals(item.Overview ?? string.Empty, expected, StringComparison.Ordinal))
            {
                continue;
            }

            item.Overview = previous.OriginalOverview;
            await item.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, cancellationToken).ConfigureAwait(false);
            _logger.LogDebug("Removed the upcoming episode message from {Name}.", item.Name);
        }
    }

    private static string GetOriginalOverview(
        Dictionary<string, InjectedOverviewState> state,
        string itemId,
        string currentOverview)
    {
        if (state.TryGetValue(itemId, out var previous)
            && string.Equals(
                currentOverview,
                ComposeOverview(previous.Message, previous.OriginalOverview),
                StringComparison.Ordinal))
        {
            return previous.OriginalOverview;
        }

        // The overview changed outside of the plugin (for example a metadata refresh), so it becomes the new baseline.
        return currentOverview;
    }
}
