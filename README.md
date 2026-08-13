# Upcoming Episodes (Jellyfin plugin)

Queries the [Sonarr](https://sonarr.tv) calendar on a nightly schedule and writes a short
"next episode" message onto the matching series in the Jellyfin library.

Requires Jellyfin 10.11 or newer.

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

## Installing

### From the plugin catalog (recommended)

Dashboard → Plugins → Repositories → **+**, then add:

```text
https://raw.githubusercontent.com/stephenmann/upcoming-episodes-plugin/main/manifest.json
```

The plugin then shows up under Dashboard → Plugins → Catalog → Metadata. Jellyfin downloads
the release zip listed in the manifest, verifies its MD5 checksum, and extracts it into the
plugins folder. Restart the server to finish the installation.

### Manually

```powershell
dotnet build .\Jellyfin.Plugin.UpcomingEpisodes\Jellyfin.Plugin.UpcomingEpisodes.csproj -c Release
```

Copy `Jellyfin.Plugin.UpcomingEpisodes.dll` from `bin\Release\net9.0` into
`<jellyfin data>/plugins/Upcoming Episodes/` and restart the server.

## Releasing

Releases are produced by [.github/workflows/release.yml](.github/workflows/release.yml) when a
`v*` tag is pushed:

```powershell
git tag -a v1.0.0.0 -m "Release" -m "Initial release."
git push origin v1.0.0.0
```

The workflow builds the plugin with the version from the tag, packages
`Jellyfin.Plugin.UpcomingEpisodes.dll` plus a generated `meta.json` into
`upcoming-episodes_<version>.zip`, publishes it as a GitHub release asset, and commits a new
entry (with the MD5 checksum and download URL) to `manifest.json` on `main`.

The tag annotation body is used as the changelog. `manifest.json` holds the static plugin
metadata — guid, name, description, overview, owner and category — and is the single source of
truth for both the catalog entry and the bundled `meta.json`.

## Scheduled task

The task appears as **Refresh upcoming episode messages** under Dashboard → Scheduled Tasks
and can be run on demand. Saving the configuration replaces its trigger with a daily one at the
configured hour and minute.
