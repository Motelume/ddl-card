# DDLCard

DDLCard is a local-first deadline dashboard for Windows 11 and Android/ColorOS 16. It keeps upcoming work visible as a responsive desktop card instead of hiding it inside a calendar.

## Current status

The Windows application is under active development. The first release targets:

- tasks with subtasks, progress and four priority levels;
- a responsive, resizable desktop card sorted by due time;
- human-readable countdowns down to the minute;
- local reminders, quick completion and postponement;
- themes, preset codes and cross-platform JSON backup;
- tray/background operation, optional always-on-top and edge collapse.

Android will be added after the Windows MVP is stable. There is deliberately no account system or automatic sync.

## Build

Requirements: Windows 11 and .NET 10 SDK.

```powershell
dotnet restore
dotnet test
dotnet run --project src/DDLCard.Windows
```

## Repository layout

- `src/DDLCard.Core`: platform-independent models and rules
- `src/DDLCard.Infrastructure`: SQLite persistence, backup and settings
- `src/DDLCard.Windows`: WPF Windows application
- `tests/DDLCard.Tests`: automated tests
- `docs`: product decisions and cross-platform contracts

## License

MIT

