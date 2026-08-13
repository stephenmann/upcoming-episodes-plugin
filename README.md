# Upcoming Episodes (Jellyfin plugin)

Queries the [Sonarr](https://sonarr.tv) calendar on a nightly schedule and writes a short
"next episode" message onto the matching series in the Jellyfin library.

## Messages

| Situation | Message |
| --- | --- |
| Airs later in the current week | `Next episode Thursday.` |
| Airs after the current week | `Next episode March 23rd.` |
| Episode number is 1, current week | `Season premiere Friday.` |
| Episode number is 1, later | `Season premiere March 5th.` |

The message is prepended to the series overview, separated by a blank line. The original
overview is stored in `plugins/configurations/Jellyfin.Plugin.UpcomingEpisodes.state.json`
and restored once the series no longer has an upcoming episode.

## Configuration

Dashboard → Plugins → Upcoming Episodes:

- **Sonarr URL** and **API key** (Sonarr → Settings → General → API Key)
- **Lookahead days** – capped at 30
- **Nightly run hour / minute** – used as the default trigger of the scheduled task
- **First day of the week** – decides whether an air date is still "this week"
- Optional switches for unmonitored episodes and episodes that already have a file

Series are matched to Sonarr by TVDB id, then IMDb id, then TMDB id, and finally by
title and production year.

## Building

```powershell
dotnet build .\Jellyfin.Plugin.UpcomingEpisodes\Jellyfin.Plugin.UpcomingEpisodes.csproj -c Release
```

Copy the produced `Jellyfin.Plugin.UpcomingEpisodes.dll` into
`<jellyfin data>/plugins/Upcoming Episodes/` and restart the server.

## Scheduled task

The task appears as **Refresh upcoming episode messages** under Dashboard → Scheduled Tasks
and can be run on demand. Saving the configuration replaces its trigger with a daily one at the
configured hour and minute.
