# Thronefall Interactive Mod

Bring your TikTok Live audience into your Thronefall kingdom! This mod spawns enemies based on gifts, coins, likes, and follows from your viewers during a TikTok Live stream.

## Features

- **Gift Mapping**: Map specific TikTok gift names to different enemy types.
- **Coin Rules**: Map diamond values to enemy tiers (e.g., 5 diamonds = 1 unit, 100 diamonds = elite units).
- **Like Rules**: Spawn enemies every N likes.
- **Follow Rules**: Spawn enemies every N follows.
- **Spawn Modes**:
  - **NightAware (Default)**: Gifts sent during the day are queued and spawned all at once when night begins. Gifts sent during the night spawn immediately.
  - **Immediate**: Gifts spawn enemies immediately, regardless of the time of day.
  - **Queue**: All gifts are queued for the next upcoming night.
- **In-Game UI**: Press **F9** to open the connection panel and live configuration.
- **Standalone Configurator**: Use `TikTokGiftsConfigurator.exe` for easy setup without opening the game.

## Installation

1. Install [BepInEx](https://thunderstore.io/c/thronefall/p/BepInEx/BepInExPack_Thronefall/) if you haven't already.
2. Download the latest release zip.
3. Extract the zip directly into your Thronefall game folder (the one that contains `Thronefall.exe`). The folder structure inside the zip matches exactly where everything needs to go.

After extraction your Thronefall folder should look like this:

```
Thronefall/
├── Thronefall.exe
└── BepInEx/
    ├── config/
    │   ├── com.raisinriotinteractive.thronefall.interactive.cfg   ← settings (auto-generated on first run)
    │   └── interactive_spawns.json                                ← enemy list (included in release)
    └── plugins/
        └── TikTokGiftsToEnemies/
            ├── TikTokGiftsToEnemies.dll                           ← the mod
            ├── TikTokGiftsConfigurator.exe                        ← standalone configurator
            └── (supporting DLLs)
```

> **Note:** `com.raisinriotinteractive.thronefall.interactive.cfg` is created automatically the first time you launch the game. You do not need to create it manually.

## Usage

### Standalone Configurator
Run `TikTokGiftsConfigurator.exe` from `BepInEx/plugins/TikTokGiftsToEnemies/`. It auto-detects your config and enemy list, and lets you set up all rules with dropdowns — no manual editing needed.

> **Windows SmartScreen warning?** This is expected for unsigned software with few downloads. Click **More info** then **Run anyway** to proceed. Alternatively, right-click the EXE → **Properties** → tick **Unblock** at the bottom → OK. The source code is fully available in this repository if you want to verify it.

### In-Game Panel
Press **F9** in-game to open the connection panel where you can enter your TikTok username, connect, and adjust settings live.

### Connecting to TikTok
1. Open the game (or the Configurator) and enter your TikTok username (e.g., `@YourName`).
2. Click **Connect**. The status will change to "Connected" once established.
3. Your Live stream must be active for the connection to work.

## Configuration Reference

Settings are stored in:
```
BepInEx/config/com.raisinriotinteractive.thronefall.interactive.cfg
```

| Section | Key | Type | Default | Description |
| :--- | :--- | :--- | :--- | :--- |
| **General** | `TikTokUsername` | `string` | | Your TikTok unique ID (with or without @). |
| **General** | `AutoConnect` | `bool` | `false` | Automatically connect to TikTok on game start. |
| **General** | `SpawnMode` | `string` | `NightAware` | When to spawn enemies: `Immediate`, `Queue`, or `NightAware`. |
| **General** | `ShowOnScreenNotifications` | `bool` | `true` | Show on-screen popup when a gift is received. |
| **Rules** | `GiftRules` | `string` | | Map specific gift names to spawns. |
| **Rules** | `CoinRules` | `string` | | Map diamond totals to spawns (fallback when no gift name matches). |
| **Rules** | `LikeRules` | `string` | `100:E Melee:1` | Spawn an enemy every N likes. |
| **Rules** | `FollowRules` | `string` | `1:E Spider Small:1` | Spawn an enemy every N follows. |

### Rule Format

All rule fields use the format `Key:EnemyName:Count` with multiple rules separated by `;`.

`EnemyName` is the prefab name from `interactive_spawns.json`. The Configurator shows all available names in a dropdown.

| Rule Field | Key meaning | Example |
| :--- | :--- | :--- |
| `GiftRules` | TikTok gift name | `Rose:E Melee:1;Lion:E Fury:2` |
| `CoinRules` | Minimum diamonds (highest match wins) | `5:E Melee:1;50:E Fury:3;200:E Ogre:5` |
| `LikeRules` | Likes required per spawn trigger | `100:E Melee:1;500:E Fury:1` |
| `FollowRules` | Follows required per spawn trigger | `1:E Weakling:1` |

#### Example Config

```
GiftRules   = Rose:E Melee:1;TikTok:E Melee:1;Universe:E Ogre:5
CoinRules   = 5:E Melee:1;50:E Fury:3;200:E Ogre:5
LikeRules   = 100:E Melee:1
FollowRules = 1:E Weakling:1
```

## Troubleshooting

- **SmartScreen says the app is unsafe**: Click **More info** → **Run anyway**. Or right-click the EXE → **Properties** → tick **Unblock** → OK. This warning appears because the executable is not yet code-signed. The full source is available in this repo.

- **Connection Error**: Ensure your `TikTokUsername` is correct and your Live stream is currently active.
- **Enemies Not Spawning**: Check that you are in a level with active waves. Ensure `SpawnMode` is set correctly — if you expect immediate spawns, use `Immediate` mode.
- **Configurator shows no enemies**: Make sure `interactive_spawns.json` is in `BepInEx/config/`. It is included in the release zip — re-extract if missing.
- **Rules Not Applying**: Verify the format in your rule fields. Ensure there are no trailing semicolons and that the enemy name matches exactly what is shown in the Configurator dropdown.

## Credits

Developed by **RaisinRiot**.  
Special thanks to the Thronefall modding community.
