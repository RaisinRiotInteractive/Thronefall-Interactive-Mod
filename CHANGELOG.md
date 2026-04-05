# Changelog

## [1.0.3] - 2026-04-05
### Fixed
- Follow rules now trigger based on stream-wide total followers (e.g. every N new followers) instead of per-user, which was always 1 and never useful.
- Default follow rule corrected from `E Spider Small` to `E Small Spider` (prefab name was swapped).
- Removed debug reflection logging that spammed BepInEx logs on every follow, gift, and like event.
- Config tab hint text updated to show correct `PrefabName` format (was incorrectly showing `SpawnIndex`).
- `SmallStyle()` now cached to avoid allocating a new GUIStyle every frame.
- Configurator deploy target now correctly installs to `plugins/TikTokGiftsToEnemies/` subfolder.
- Added `E Small Spider` to bundled enemy list.
- Added Hide button to in-game panel with F9 restore reminder.
- Spawn mode default changed to `Immediate`.
- `.claude/` added to `.gitignore`.

## [1.0.2] - 2026-04-04
### Fixed
- Configurator now loads enemy names on startup even before the game has been run for the first time (no cfg file required). Searches for `interactive_spawns.json` independently of the config file.

## [1.0.1] - 2026-04-04
### Fixed
- Bundled pre-populated `interactive_spawns.json` so all enemy names are available in the Configurator on a fresh install without needing to play a level first.
- Renamed mod to Thronefall Interactive Mod throughout (plugin name, config file, spawns file, Configurator window title).

## [1.0.0] - 2026-04-03
### Added
- Initial release of Thronefall TikTok Gifts to Enemies Mod.
- Standalone Configurator for easy gift-to-enemy mapping.
- In-game F9 panel for connection management and live configuration.
- Support for NightAware, Immediate, and Queue spawn modes.
- On-screen notifications for gift events.

### Fixed
- Fixed identity GUID to `com.raisinriot.thronefall.tiktokgifts`.
- Fixed `SpawnMode` not saving correctly in the Configurator.
- Improved Steam path auto-detection in Configurator using registry lookups.
- Resolved thread race condition in TikTok connection lifecycle.
- Improved spawn reliability during night transitions (gifts are no longer dropped if the spawner isn't ready).
- Ensured in-game configuration changes persist to disk immediately.
