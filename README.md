# Thronefall Interactive Mod

Bring your TikTok Live audience into your Thronefall kingdom! This mod spawns enemies based on gifts, coins, likes, and follows from your viewers during a TikTok Live stream.

## Features

- **Gift Mapping**: Map specific TikTok gift names to different enemy types.
- **Coin Rules**: Map diamond values to enemy tiers (e.g., 5 diamonds = basic unit, 100 diamonds = elite units).
- **Like Rules**: Spawn enemies every N likes.
- **Follow Rules**: Spawn enemies every N new followers.
- **Spawn Modes**:
  - **Immediate (Default)**: Gifts spawn enemies instantly, regardless of day or night.
  - **NightAware**: Gifts sent during the day are queued and released when night begins. Gifts sent at night spawn immediately.
  - **Queue**: All gifts are queued for the next upcoming night.
- **In-Game UI**: Press **F9** to open the connection panel and live configuration.
- **Standalone Configurator**: Use `TikTokGiftsConfigurator.exe` for easy setup without opening the game.

## Installation

1. Install [BepInEx](https://thunderstore.io/c/thronefall/p/BepInEx/BepInExPack_Thronefall/) if you haven't already.
2. Download the latest release zip from the [Releases](https://github.com/RaisinRiotInteractive/Thronefall-Interactive-Mod/releases/latest) page.
3. Extract the zip directly into your Thronefall game folder (the one containing `Thronefall.exe`).

After extraction your Thronefall folder should look like this:

```
Thronefall/
├── Thronefall.exe
└── BepInEx/
    ├── config/
    │   ├── com.raisinriotinteractive.thronefall.interactive.cfg   ← settings
    │   └── interactive_spawns.json                                ← enemy list
    └── plugins/
        └── TikTokGiftsToEnemies/
            ├── TikTokGiftsToEnemies.dll                           ← the mod
            ├── TikTokGiftsConfigurator.exe                        ← standalone configurator
            └── (supporting DLLs)
```

### Setup

Run `TikTokGiftsConfigurator.exe` from `BepInEx/plugins/TikTokGiftsToEnemies/` to configure the mod — enter your TikTok username, map gifts to enemies, and set your spawn preferences. No need to launch the game first.

> **Windows SmartScreen warning?** Click **More info** then **Run anyway**. Or right-click the EXE → **Properties** → tick **Unblock** → OK.

For a full setup guide including TikTok connection, rule configuration, and troubleshooting, see the [Setup Guide](SETUP.md).

## Credits

Developed by **RaisinRiot**.  
Special thanks to the Thronefall modding community.
