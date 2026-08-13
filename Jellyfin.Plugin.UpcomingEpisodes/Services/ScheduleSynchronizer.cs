using Jellyfin.Plugin.UpcomingEpisodes.Configuration;
using Jellyfin.Plugin.UpcomingEpisodes.ScheduledTasks;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.UpcomingEpisodes.Services;

/// <summary>
/// Keeps the trigger of the nightly task in sync with the configured run time.
/// </summary>
public class ScheduleSynchronizer : IHostedService
{
    private readonly ITaskManager _taskManager;
    private readonly ILogger<ScheduleSynchronizer> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScheduleSynchronizer"/> class.
    /// </summary>
    /// <param name="taskManager">Instance of the <see cref="ITaskManager"/> interface.</param>
    /// <param name="logger">Instance of the <see cref="ILogger{ScheduleSynchronizer}"/> interface.</param>
    public ScheduleSynchronizer(ITaskManager taskManager, ILogger<ScheduleSynchronizer> logger)
    {
        _taskManager = taskManager;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (Plugin.Instance is not null)
        {
            Plugin.Instance.ConfigurationChanged += OnConfigurationChanged;
            ApplySchedule(Plugin.Instance.Configuration);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        if (Plugin.Instance is not null)
        {
            Plugin.Instance.ConfigurationChanged -= OnConfigurationChanged;
        }

        return Task.CompletedTask;
    }

    private void OnConfigurationChanged(object? sender, BasePluginConfiguration configuration)
    {
        if (configuration is PluginConfiguration pluginConfiguration)
        {
            ApplySchedule(pluginConfiguration);
        }
    }

    private void ApplySchedule(PluginConfiguration configuration)
    {
        var worker = _taskManager.ScheduledTasks
            .FirstOrDefault(task => task.ScheduledTask is RefreshUpcomingEpisodesTask);

        if (worker is null)
        {
            return;
        }

        var timeOfDay = new TimeSpan(
            Math.Clamp(configuration.ScheduleHour, 0, 23),
            Math.Clamp(configuration.ScheduleMinute, 0, 59),
            0);

        var existing = worker.Triggers.ToList();
        if (existing.Count == 1
            && existing[0].Type == TaskTriggerInfoType.DailyTrigger
            && existing[0].TimeOfDayTicks == timeOfDay.Ticks)
        {
            return;
        }

        worker.Triggers = new[]
        {
            new TaskTriggerInfo
            {
                Type = TaskTriggerInfoType.DailyTrigger,
                TimeOfDayTicks = timeOfDay.Ticks
            }
        };

        _logger.LogInformation("The upcoming episodes task now runs daily at {TimeOfDay}.", timeOfDay);
    }
}
