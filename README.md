# Activity Dashboard

A Playnite 6 extension that adds a personal activity dashboard to the sidebar.

## Features

- A 52-week heatmap generated from tracked game sessions.
- A 24-hour radial chart that highlights your preferred gaming hours.
- A 12-month playtime trend chart and a weekday distribution chart.
- Total playtime, played games, launches, and games active in the past 30 days.
- Highlights section with current and longest streak, average session length,
  longest session, most active weekday, weekend ratio and first tracked session.
- Rankings for games, platforms, and genres.
- Session length distribution that buckets your tracked sessions.
- Recent session history and an option to clear only the extension's tracked data.
- Anchor navigation (Overview, Activity, Library, Sessions) to jump between sections.

## Dashboard sections

| Section   | Contents |
|-----------|----------|
| Overview  | Hero greeting, refresh button, 52-week heatmap, four key metric cards, 24-hour radial chart, eight highlight cards. |
| Activity  | Monthly bars, weekday distribution, session length distribution, daily pattern stats. |
| Library   | Most played games (all-time + period), platform and genre breakdown, favorite games. |
| Sessions  | Longest session, recent monthly peak, recent session list. |

## Session history

Playnite exposes game totals and last activity, but not the complete session history. The heatmap and hourly chart therefore start recording after the extension is installed. Dashboard cards and rankings still use the full playtime already available in the Playnite library.

## Development

```powershell
dotnet build src/Dashboard.csproj
dotnet test tests/Dashboard.Tests/Dashboard.Tests.csproj
```

Copy `Dashboard.dll` and its dependencies, together with `extension.yaml`, to the Playnite extensions folder.
