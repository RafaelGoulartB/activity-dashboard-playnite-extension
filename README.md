# Activity Dashboard

A Playnite 6 extension that adds a personal activity dashboard to the sidebar.

## Features

- A 52-week heatmap generated from tracked game sessions.
- A 24-hour radial chart that highlights your preferred gaming hours.
- Total playtime, played games, launches, and games active in the past 30 days.
- Rankings for games, platforms, and genres.
- Recent session history and an option to clear only the extension's tracked data.

## Session history

Playnite exposes game totals and last activity, but not the complete session history. The heatmap and hourly chart therefore start recording after the extension is installed. Dashboard cards and rankings still use the full playtime already available in the Playnite library.

## Development

```powershell
dotnet build src/Dashboard.csproj
dotnet test tests/Dashboard.Tests/Dashboard.Tests.csproj
```

Copy `Dashboard.dll` and its dependencies, together with `extension.yaml`, to the Playnite extensions folder.

