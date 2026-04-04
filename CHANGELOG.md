# Changelog

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
