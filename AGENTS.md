# Activity Dashboard contributor guide

## Project overview

Activity Dashboard is a Playnite 6 `GenericPlugin` that records sessions launched through Playnite and presents library activity in a WPF sidebar dashboard. It targets .NET Framework 4.6.2 and uses C# 6-compatible code.

## Commands

```powershell
dotnet build src/Dashboard.csproj
dotnet test tests/Dashboard.Tests/Dashboard.Tests.csproj
```

The project outputs `Dashboard.dll` and required dependencies to the repository root for Playnite deployment.

## Architecture

```text
DashboardPlugin
├── SessionTracker + JsonActivityStore
│   └── activity-dashboard.json in Playnite ExtensionsDataPath
├── DashboardAnalytics
│   ├── 52-week daily heatmap
│   ├── hourly activity aggregation
│   └── library rankings and summary cards
└── DashboardView / DashboardViewModel
    └── WPF sidebar interface and HourlyActivityChart custom control
```

## Implementation rules

- Keep visible UI, tooltips, notifications, settings, and documentation in English.
- Keep gameplay data local. Never modify Playnite game records to store dashboard sessions.
- Treat `OnGameStopped.ElapsedSeconds` as the authoritative session duration.
- Split sessions at midnight for heatmap data and at each hour for the radial chart.
- Use local `DateTimeOffset` wall-clock values for activity visualizations.
- Do not introduce charting frameworks; the radial hourly chart is a lightweight custom WPF `FrameworkElement`.
- Keep long-running analytics work off the UI thread. Update WPF-bound collections only after calculation completes.
- Preserve compatibility with .NET Framework 4.6.2 and avoid C# features newer than C# 6.

## Tests

Add or update unit tests whenever analytics, persistence, or session behavior changes. Cover empty data, malformed persistence data, duplicate lifecycle events, zero-duration sessions, midnight splits, hour splits, and aggregation changes.
