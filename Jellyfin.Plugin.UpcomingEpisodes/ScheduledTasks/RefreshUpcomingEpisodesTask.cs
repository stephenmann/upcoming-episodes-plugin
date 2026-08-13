using Jellyfin.Plugin.UpcomingEpisodes.Services;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.UpcomingEpisodes.ScheduledTasks;

/// <summary>
/// Nightly task that refreshes the upcoming episode messages.
/// </summary>
public class RefreshUpcomingEpisodesTask : IScheduledTask, IConfigurableScheduledTask
{
    private readonly UpcomingEpisodesService _service;
    private readonly ILogger<RefreshUpcomingEpisodesTask> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RefreshUpcomingEpisodesTask"/> class.
    /// </summary>
    /// <param name="service">The upcoming episodes service.</param>
    /// <param name="logger">Instance of the <see cref="ILogger{RefreshUpcomingEpisodesTask}"/> interface.</param>
    public RefreshUpcomingEpisodesTask(UpcomingEpisodesService service, ILogger<RefreshUpcomingEpisodesTask> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <inheritdoc />
    public string Name => "Refresh upcoming episode messages";

    /// <inheritdoc />
    public string Key => "UpcomingEpisodesRefresh";

    /// <inheritdoc />
    public string Description => "Queries the Sonarr calendar and updates the next episode message on each series.";

    /// <inheritdoc />
    public string Category => "Upcoming Episodes";

    /// <inheritdoc />
    public bool IsHidden => false;

    /// <inheritdoc />
    public bool IsEnabled => true;

    /// <inheritdoc />
    public bool IsLogged => true;

    /// <inheritdoc />
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        var configuration = Plugin.Instance?.Configuration;
        var hour = Math.Clamp(configuration?.ScheduleHour ?? 3, 0, 23);
        var minute = Math.Clamp(configuration?.ScheduleMinute ?? 0, 0, 59);

        yield return new TaskTriggerInfo
        {
            Type = TaskTriggerInfo.TriggerDaily,
            TimeOfDayTicks = new TimeSpan(hour, minute, 0).Ticks
        };
    }

    /// <inheritdoc />
    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        try
        {
            await _service.RefreshAsync(progress, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Refreshing the upcoming episode messages failed.");
            throw;
        }
    }
}
