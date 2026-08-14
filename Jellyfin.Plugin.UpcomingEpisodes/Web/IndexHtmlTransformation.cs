using System.Globalization;

namespace Jellyfin.Plugin.UpcomingEpisodes.Web;

/// <summary>
/// Injects the client script into index.html. Invoked by the File Transformation plugin.
/// </summary>
public static class IndexHtmlTransformation
{
    private const string Marker = "<!-- upcoming-episodes -->";

    /// <summary>
    /// Gets a value indicating whether the File Transformation plugin has served a transformed index.html.
    /// </summary>
    public static bool HasRun { get; private set; }

    /// <summary>
    /// Gets or sets the sink used to report the first invocation, since the callback is static.
    /// </summary>
    internal static Action<string, string>? Report { get; set; }

    /// <summary>
    /// Adds the client script to the served index.html.
    /// </summary>
    /// <param name="payload">The current file contents.</param>
    /// <returns>The transformed contents.</returns>
    public static string Transform(TransformationPayload payload)
    {
        var contents = payload?.Contents ?? string.Empty;

        if (!HasRun)
        {
            HasRun = true;
            Report?.Invoke(contents.Length == 0 ? "empty" : "ok", ClientScript.Url);
        }

        if (contents.Length == 0 || contents.Contains(Marker, StringComparison.Ordinal))
        {
            return contents;
        }

        // async keeps the script off the critical path: it neither blocks parsing nor delays
        // DOMContentLoaded, which the webOS and Android wrappers hook to boot the web client.
        var injection = string.Format(
            CultureInfo.InvariantCulture,
            "{0}<script src=\"{1}\" async></script>",
            Marker,
            ClientScript.Url);

        var closingBody = contents.LastIndexOf("</body>", StringComparison.OrdinalIgnoreCase);
        return closingBody < 0
            ? contents + injection
            : contents.Insert(closingBody, injection);
    }
}
