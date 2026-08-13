using System.Globalization;
using System.Reflection;
using System.Text;

namespace Jellyfin.Plugin.UpcomingEpisodes.Web;

/// <summary>
/// Injects the client script into index.html. Invoked by the File Transformation plugin.
/// </summary>
public static class IndexHtmlTransformation
{
    private const string Marker = "<!-- upcoming-episodes -->";

    private static readonly Lazy<string> _script = new(ReadScript);

    /// <summary>
    /// Adds the client script to the served index.html.
    /// </summary>
    /// <param name="payload">The current file contents.</param>
    /// <returns>The transformed contents.</returns>
    public static string Transform(TransformationPayload payload)
    {
        var contents = payload?.Contents ?? string.Empty;
        if (contents.Length == 0 || contents.Contains(Marker, StringComparison.Ordinal))
        {
            return contents;
        }

        var injection = string.Format(
            CultureInfo.InvariantCulture,
            "{0}<script>{1}</script>",
            Marker,
            _script.Value);

        var closingBody = contents.LastIndexOf("</body>", StringComparison.OrdinalIgnoreCase);
        return closingBody < 0
            ? contents + injection
            : contents.Insert(closingBody, injection);
    }

    private static string ReadScript()
    {
        var assembly = typeof(IndexHtmlTransformation).Assembly;
        using var stream = assembly.GetManifestResourceStream(
            "Jellyfin.Plugin.UpcomingEpisodes.Web.upcomingEpisodes.js");

        if (stream is null)
        {
            return string.Empty;
        }

        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }
}
