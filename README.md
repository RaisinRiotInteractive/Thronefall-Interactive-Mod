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
2. Download the latest release.
3. Extract the contents into your `Thronefall/BepInEx/plugins` folder.
4. Run the game once to generate the config file and the `interactive_spawns.json` enemy list.

## Usage

### Connecting to TikTok
1. Open the game and press **F9**.
2. Enter your TikTok username (e.g., `@YourName`).
3. Click **Connect**. The status will change to "Connected" once established.

### Configurator
Run `TikTokGiftsConfigurator.exe` from the plugins folder. It will auto-detect your config file, load available enemy names from `interactive_spawns.json`, and let you build rules with dropdowns — no manual editing required.

### Configuration Reference

All settings are stored in `BepInEx/config/com.raisinriot.thronefall.tiktokgifts.cfg`.

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

`EnemyName` is the prefab name from `interactive_spawns.json` — run the game once to generate this file, then open the Configurator or check it directly for valid names.

| Rule Field | Key meaning | Example |
| :--- | :--- | :--- |
| `GiftRules` | TikTok gift name | `Rose:E Melee:1;Lion:E Elite:2` |
| `CoinRules` | Minimum diamonds (highest match wins) | `5:E Melee:1;50:E Elite:3;200:E Boss:5` |
| `LikeRules` | Likes required per spawn trigger | `100:E Melee:1;500:E Elite:1` |
| `FollowRules` | Follows required per spawn trigger | `1:E Spider Small:1` |

#### Example

```
GiftRules   = Rose:E Melee:1;TikTok:E Melee:1;Universe:E Boss:5
CoinRules   = 5:E Melee:1;50:E Elite:3;200:E Boss:5
LikeRules   = 100:E Melee:1
FollowRules = 1:E Spider Small:1
```

With these rules:
- A **Rose** gift spawns 1 basic enemy.
- A **Universe** gift spawns 5 boss enemies.
- An unknown gift worth **60 diamonds** matches the `50` threshold and spawns 3 elite units.
- Every **100 likes** spawns 1 basic enemy.
- Every **follow** spawns a small spider.

## Troubleshooting

- **Connection Error**: Ensure your `TikTokUsername` is correct and your Live stream is currently active.
- **Enemies Not Spawning**: Check that you are in a level with active waves. Ensure `SpawnMode` is set correctly — if you expect immediate spawns, use `Immediate` mode.
- **Enemy Name Not Found**: Run the game and play a level to generate `interactive_spawns.json` in `BepInEx/config/`. Open the Configurator to browse valid enemy names for the current map. Enemy availability varies by map and wave.
- **Rules Not Applying**: Verify the format in your rule fields. Ensure there are no trailing semicolons and that the enemy name matches exactly what's in `interactive_spawns.json`.

## Credits

Developed by **RaisinRiot**.  
Special thanks to the Thronefall modding community.
