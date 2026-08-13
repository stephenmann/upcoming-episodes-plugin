using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.UpcomingEpisodes.Web;

/// <summary>
/// Registers the client script with the File Transformation plugin when it is installed.
/// </summary>
public class FileTransformationRegistrar : IHostedService
{
    private const string TransformationId = "6b3f0d21-8c47-4b52-9c2e-1f7d5a90c4e8";

    private readonly ILogger<FileTransformationRegistrar> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileTransformationRegistrar"/> class.
    /// </summary>
    /// <param name="logger">Instance of the <see cref="ILogger{FileTransformationRegistrar}"/> interface.</param>
    public FileTransformationRegistrar(ILogger<FileTransformationRegistrar> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Gets a value indicating whether the client script was registered successfully.
    /// </summary>
    public bool IsRegistered { get; private set; }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            IsRegistered = Register();
        }
        catch (Exception ex) when (ex is TargetInvocationException or MissingMethodException or InvalidOperationException)
        {
            _logger.LogWarning(ex, "Registering with the File Transformation plugin failed.");
            IsRegistered = false;
        }

        if (IsRegistered)
        {
            _logger.LogInformation("Messages are shown next to the star rating via the File Transformation plugin.");
        }
        else
        {
            _logger.LogInformation("The File Transformation plugin is unavailable, messages are added to the series overview.");
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private bool Register()
    {
        var pluginInterface = AssemblyLoadContext.All
            .SelectMany(context => context.Assemblies)
            .FirstOrDefault(assembly => assembly.FullName?.Contains(".FileTransformation", StringComparison.Ordinal) == true)
            ?.GetType("Jellyfin.Plugin.FileTransformation.PluginInterface");

        var registerTransformation = pluginInterface?.GetMethod("RegisterTransformation");
        if (registerTransformation is null)
        {
            return false;
        }

        var payloadJson = JsonSerializer.Serialize(new Dictionary<string, string>
        {
            ["id"] = TransformationId,
            ["fileNamePattern"] = "index.html",
            ["callbackAssembly"] = typeof(IndexHtmlTransformation).Assembly.FullName!,
            ["callbackClass"] = typeof(IndexHtmlTransformation).FullName!,
            ["callbackMethod"] = nameof(IndexHtmlTransformation.Transform)
        });

        // The payload type is the JObject of the copy of Newtonsoft.Json loaded by that plugin.
        var payloadType = registerTransformation.GetParameters()[0].ParameterType;
        var parse = payloadType.GetMethod("Parse", BindingFlags.Public | BindingFlags.Static, new[] { typeof(string) });
        if (parse is null)
        {
            _logger.LogWarning("The File Transformation plugin expects an unsupported payload type.");
            return false;
        }

        registerTransformation.Invoke(null, new[] { parse.Invoke(null, new object[] { payloadJson }) });
        return true;
    }
}
