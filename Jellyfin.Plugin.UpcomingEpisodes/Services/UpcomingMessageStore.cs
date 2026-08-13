using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using MediaBrowser.Common.Configuration;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.UpcomingEpisodes.Services;

/// <summary>
/// Holds the current message per series so the web client can request them.
/// </summary>
public class UpcomingMessageStore
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly string _filePath;
    private readonly ILogger<UpcomingMessageStore> _logger;
    private ConcurrentDictionary<string, string> _messages;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpcomingMessageStore"/> class.
    /// </summary>
    /// <param name="applicationPaths">Instance of the <see cref="IApplicationPaths"/> interface.</param>
    /// <param name="logger">Instance of the <see cref="ILogger{UpcomingMessageStore}"/> interface.</param>
    public UpcomingMessageStore(IApplicationPaths applicationPaths, ILogger<UpcomingMessageStore> logger)
    {
        _filePath = Path.Combine(
            applicationPaths.PluginConfigurationsPath,
            "Jellyfin.Plugin.UpcomingEpisodes.messages.json");
        _logger = logger;
        _messages = Load();
    }

    /// <summary>
    /// Gets every message, keyed by the item id in "N" format.
    /// </summary>
    /// <returns>The messages.</returns>
    public IReadOnlyDictionary<string, string> GetAll() => _messages;

    /// <summary>
    /// Replaces all messages and persists them.
    /// </summary>
    /// <param name="messages">The new messages, keyed by item id in "N" format.</param>
    public void Replace(IDictionary<string, string> messages)
    {
        _messages = new ConcurrentDictionary<string, string>(messages, StringComparer.OrdinalIgnoreCase);

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
            File.WriteAllText(_filePath, JsonSerializer.Serialize(_messages, _jsonOptions), Encoding.UTF8);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogError(ex, "Unable to write {Path}.", _filePath);
        }
    }

    private ConcurrentDictionary<string, string> Load()
    {
        if (!File.Exists(_filePath))
        {
            return new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            var json = File.ReadAllText(_filePath, Encoding.UTF8);
            var messages = JsonSerializer.Deserialize<Dictionary<string, string>>(json, _jsonOptions);
            return new ConcurrentDictionary<string, string>(
                messages ?? new Dictionary<string, string>(),
                StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            _logger.LogWarning(ex, "Unable to read {Path}, starting with no messages.", _filePath);
            return new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }
}
