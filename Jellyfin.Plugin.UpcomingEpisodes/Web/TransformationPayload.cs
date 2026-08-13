namespace Jellyfin.Plugin.UpcomingEpisodes.Web;

/// <summary>
/// Payload handed to the transformation callback by the File Transformation plugin.
/// </summary>
public class TransformationPayload
{
    /// <summary>
    /// Gets or sets the current contents of the file being served.
    /// </summary>
    public string Contents { get; set; } = string.Empty;
}
