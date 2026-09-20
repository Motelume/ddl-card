# DDLCard

DDLCard is a local-first deadline dashboard for Windows 11 and Android/ColorOS 16. It keeps upcoming work visible as a desktop card or home-screen widget instead of hiding it inside a calendar.

## Included in v0.1.0

- Tasks, subtasks, progress, categories and four priority levels.
- Human-readable countdowns down to the minute, sorted by nearest deadline.
- Local notifications, quick completion and postponement.
- Windows responsive card, tray operation, global quick add and optional edge collapse.
- Android manager plus fixed 4x4 and 4x6 Glance home-screen widgets.
- Compatible preset codes and versioned JSON backup for optional manual transfer.
- Fully local storage: no account, analytics, server or automatic sync.

## Build

### Windows

Requires Windows 11 and the .NET 10 SDK.

```powershell
dotnet restore DDLCard.slnx
dotnet test DDLCard.slnx
.\scripts\publish-windows.ps1
```

### Android

Requires JDK 17 and Android SDK Platform 36.

```powershell
cd android
.\gradlew.bat testDebugUnitTest assembleDebug
```

The installable debug APK is written to `android/app/build/outputs/apk/debug/app-debug.apk`.

## Repository layout

- `src/DDLCard.Core`: platform-independent Windows models and rules
- `src/DDLCard.Infrastructure`: Windows SQLite persistence, backup and settings
- `src/DDLCard.Windows`: WPF Windows application
- `android`: Kotlin, Compose and Glance Android application
- `tests/DDLCard.Tests`: automated Windows tests
- `docs`: product decisions and user guides

See the [Windows guide](docs/WINDOWS_USER_GUIDE.md) and [Android guide](docs/ANDROID_USER_GUIDE.md).

## License

MIT
