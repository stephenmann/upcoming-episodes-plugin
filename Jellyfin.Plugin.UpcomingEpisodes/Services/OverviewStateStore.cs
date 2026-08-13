using System.Globalization;
using System.Text;
using System.Text.Json;
using MediaBrowser.Common.Configuration;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.UpcomingEpisodes.Services;

/// <summary>
/// Stores, per series, the overview that existed before a message was written to it.
/// </summary>
public class OverviewStateStore
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly string _stateFilePath;
    private readonly ILogger<OverviewStateStore> _logger;
    private Dictionary<string, InjectedOverviewState>? _state;

    /// <summary>
    /// Initializes a new instance of the <see cref="OverviewStateStore"/> class.
    /// </summary>
    /// <param name="applicationPaths">Instance of the <see cref="IApplicationPaths"/> interface.</param>
    /// <param name="logger">Instance of the <see cref="ILogger{OverviewStateStore}"/> interface.</param>
    public OverviewStateStore(IApplicationPaths applicationPaths, ILogger<OverviewStateStore> logger)
    {
        _stateFilePath = Path.Combine(
            applicationPaths.PluginConfigurationsPath,
            "Jellyfin.Plugin.UpcomingEpisodes.state.json");
        _logger = logger;
    }

    /// <summary>
    /// Loads the state from disk, or returns the already loaded state.
    /// </summary>
    /// <returns>The state keyed by item id.</returns>
    public Dictionary<string, InjectedOverviewState> Load()
    {
        if (_state is not null)
        {
            return _state;
        }

        if (!File.Exists(_stateFilePath))
        {
            _state = new Dictionary<string, InjectedOverviewState>(StringComparer.OrdinalIgnoreCase);
            return _state;
        }

        try
        {
            var json = File.ReadAllText(_stateFilePath, Encoding.UTF8);
            _state = JsonSerializer.Deserialize<Dictionary<string, InjectedOverviewState>>(json, _jsonOptions)
                     ?? new Dictionary<string, InjectedOverviewState>(StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            _logger.LogWarning(ex, "Unable to read {Path}, starting with empty state.", _stateFilePath);
            _state = new Dictionary<string, InjectedOverviewState>(StringComparer.OrdinalIgnoreCase);
        }

        return _state;
    }

    /// <summary>
    /// Writes the state to disk.
    /// </summary>
    /// <param name="state">The state to persist.</param>
    public void Save(Dictionary<string, InjectedOverviewState> state)
    {
        _state = state;

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_stateFilePath)!);
            File.WriteAllText(_stateFilePath, JsonSerializer.Serialize(state, _jsonOptions), Encoding.UTF8);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogError(
                ex,
                "Unable to write {Path}. {Count} series states were not persisted.",
                _stateFilePath,
                state.Count.ToString(CultureInfo.InvariantCulture));
        }
    }
}
