using Jellyfin.Plugin.UpcomingEpisodes.Services;
using Jellyfin.Plugin.UpcomingEpisodes.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.UpcomingEpisodes.Api;

/// <summary>
/// Exposes the upcoming episode messages to the web client.
/// </summary>
[ApiController]
[Authorize]
[Route("UpcomingEpisodes")]
public class UpcomingEpisodesController : ControllerBase
{
    private readonly UpcomingMessageStore _messageStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpcomingEpisodesController"/> class.
    /// </summary>
    /// <param name="messageStore">The message store.</param>
    public UpcomingEpisodesController(UpcomingMessageStore messageStore)
    {
        _messageStore = messageStore;
    }

    /// <summary>
    /// Gets the current message for every series that has an upcoming episode.
    /// </summary>
    /// <response code="200">Messages returned, keyed by item id.</response>
    /// <returns>The messages.</returns>
    [HttpGet("Messages")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyDictionary<string, string>> GetMessages()
    {
        return Ok(_messageStore.GetAll());
    }

    /// <summary>
    /// Gets the script index.html loads. Anonymous because index.html itself carries no access token.
    /// </summary>
    /// <response code="200">The script.</response>
    /// <returns>The script.</returns>
    [HttpGet("Script.js")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult GetScript()
    {
        // The url is content addressed, so the response never goes stale.
        Response.Headers.CacheControl = "public, max-age=31536000, immutable";
        return Content(ClientScript.Contents, "application/javascript; charset=utf-8");
    }
}
