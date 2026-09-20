# DDLCard development plan

## Product decisions

- Windows 11 first; Android/ColorOS 16 second; wallpaper mode after Android.
- Local-first and offline. No accounts, server or automatic device sync.
- One responsive card per device. It shows as many tasks as fit and sorts active tasks by due time.
- Countdown format is `days + hours + minutes`; days are omitted below one day and seconds are never shown.
- Overdue tasks remain visible until completed, postponed or archived.
- Tasks support subtasks, automatic subtask progress, manual progress without subtasks, and Low/Normal/High/Urgent priority.
- Reminders have defaults (7 days, 3 days, 24 hours, 1 hour, 10 minutes) and per-task customization.
- Preset codes copy appearance and reminder defaults only. Import always previews before applying.
- JSON backup is versioned and cross-platform so data can be moved manually without adding sync.

## Delivery milestones

1. **Foundation:** repository, contracts, core rules and automated tests.
2. **Local data:** SQLite repositories, settings, preset codes and versioned JSON backup.
3. **Windows manager:** list, create/edit, quick add, subtasks, completion, archive and postponement.
4. **Desktop card:** responsive rows, countdown refresh, drag/resize, desktop/topmost modes and edge collapse.
5. **Background behavior:** tray icon, global quick-add shortcut, reminders and startup registration.
6. **Windows release:** automated tests, smoke test checklist and self-contained package.
7. **Android:** Kotlin/Compose app, Room persistence, fixed 4x4 and 4x6 Glance widgets, notification onboarding.
8. **Wallpaper:** renderer interface implementations for Windows and Android after both card surfaces are stable.

## Definition of done for Windows MVP

- Fresh install can create, edit, complete, postpone, archive, export and restore tasks without network access.
- Closing the main window keeps the tray process and desktop card running.
- Card size changes the number of visible rows; the nearest deadline remains first.
- Edge-docked card reveals on pointer approach and can be pinned open.
- Reminders fire once per configured reminder and survive app restarts.
- Corrupt imports and preset codes are rejected without overwriting current data.
- Core and persistence tests pass; a self-contained Windows x64 build launches on Windows 11.

