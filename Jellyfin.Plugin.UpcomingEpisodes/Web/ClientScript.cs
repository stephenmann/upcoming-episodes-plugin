using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Jellyfin.Plugin.UpcomingEpisodes.Web;

/// <summary>
/// The script the web client loads to render the message next to the star rating.
/// </summary>
public static class ClientScript
{
    private const string ResourceName = "Jellyfin.Plugin.UpcomingEpisodes.Web.upcomingEpisodes.js";

    private static readonly Lazy<string> _contents = new(Read);
    private static readonly Lazy<string> _url = new(BuildUrl);

    /// <summary>
    /// Gets the script contents.
    /// </summary>
    public static string Contents => _contents.Value;

    /// <summary>
    /// Gets the url index.html points at. It is relative to /web/ so that it also resolves when the
    /// server runs under a base url, and carries a content hash so a new build is never served stale.
    /// </summary>
    public static string Url => _url.Value;

    private static string Read()
    {
        using var stream = typeof(ClientScript).Assembly.GetManifestResourceStream(ResourceName);

        if (stream is null)
        {
            return string.Empty;
        }

        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    private static string BuildUrl()
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Contents)));

        return string.Format(
            CultureInfo.InvariantCulture,
            "../UpcomingEpisodes/Script.js?v={0}",
            hash[..8].ToLowerInvariant());
    }
}
