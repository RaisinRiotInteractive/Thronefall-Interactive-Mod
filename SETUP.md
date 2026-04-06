# Setup Guide — Thronefall Interactive Mod

## Installation

### Option 1 — Installer (Recommended)

1. Download `ThronefallInteractive-Installer.exe` from the [Releases](https://github.com/RaisinRiotInteractive/Thronefall-Interactive-Mod/releases/latest) page.
2. Run it — it will auto-detect your Thronefall folder, download and install BepInEx if needed, then install the mod.
3. Click **Launch Configurator** when done to set up your TikTok username and rules.

> **Windows SmartScreen warning?** Click **More info** then **Run anyway**. Or right-click → **Properties** → tick **Unblock** → OK. The full source is available in this repository.

### Option 2 — Manual (Zip)

1. Download and install [BepInExPack for Thronefall](https://thunderstore.io/c/thronefall/p/BepInEx/BepInExPack_Thronefall/) from Thunderstore. Extract it into your Thronefall game folder (the one containing `Thronefall.exe`).
2. Download the latest `Thronefall-Interactive-Mod-x.x.x.zip` from the [Releases](https://github.com/RaisinRiotInteractive/Thronefall-Interactive-Mod/releases/latest) page and extract it directly into your Thronefall game folder.

After extraction your Thronefall folder should look like this:

```
Thronefall/
├── Thronefall.exe
└── BepInEx/
    ├── config/
    │   ├── com.raisinriotinteractive.thronefall.interactive.cfg   ← settings
    │   └── interactive_spawns.json                                ← enemy list
    └── plugins/
        └── ThronefallInteractive/
            ├── ThronefallInteractive.dll                           ← the mod
            ├── ThronefallInteractiveConfigurator.exe                        ← standalone configurator
            └── (supporting DLLs)
```

---

## Configuration

### Using the Standalone Configurator (Recommended)

Run `ThronefallInteractiveConfigurator.exe` from `BepInEx/plugins/ThronefallInteractive/`. It auto-detects your config and enemy list and lets you set up all rules with dropdowns — no manual editing needed. A default config is bundled with the mod so you can configure everything without launching the game first.

> **Windows SmartScreen warning?** Click **More info** then **Run anyway**. Alternatively, right-click the EXE → **Properties** → tick **Unblock** → OK. The full source code is available in this repository.

### Using the In-Game Panel

Press **F9** in-game to open the connection panel. From here you can enter your TikTok username, connect, and adjust rules live. Press **F9** again or click **Hide** to close it.

---

## Connecting to TikTok

1. Enter your TikTok username (e.g. `@YourName`) in the Configurator or the in-game F9 panel. Your username is automatically converted to lowercase — no need to worry about capitalisation.
2. Click **Connect**. The status will change to "Connected" once established.
3. Your TikTok Live stream must be active for the connection to work.

---

## Rule Configuration

All rules use the format `Key:EnemyName:Count` with multiple rules separated by `;`.

`EnemyName` is the prefab name from `interactive_spawns.json`. The Configurator shows all available names in a dropdown.

| Rule | Key meaning | Example |
| :--- | :--- | :--- |
| **Gift Rules** | TikTok gift name | `Rose:E Melee:1;Universe:E Ogre:5` |
| **Coin Rules** | Minimum diamonds (highest match wins) | `5:E Melee:1;50:E Fury:3;200:E Ogre:5` |
| **Like Rules** | Likes required per spawn trigger | `100:E Melee:1;500:E Fury:1` |
| **Follow Rules** | Stream total follows required per trigger | `1:E Small Spider:1` |

### Example Config

```
GiftRules   = Rose:E Melee:1;Universe:E Ogre:5
CoinRules   = 5:E Melee:1;50:E Fury:3;200:E Ogre:5
LikeRules   = 100:E Melee:1
FollowRules = 1:E Small Spider:1
```

### Spawn Modes

| Mode | Behaviour |
| :--- | :--- |
| **Immediate** (default) | Enemies spawn instantly when a gift is received |
| **NightAware** | Queued during the day, released at the start of the next night |

---

## Settings Reference

Settings are stored in:
```
BepInEx/config/com.raisinriotinteractive.thronefall.interactive.cfg
```

| Section | Key | Type | Default | Description |
| :--- | :--- | :--- | :--- | :--- |
| General | `TikTokUsername` | string | | Your TikTok unique ID (with or without @). |
| General | `AutoConnect` | bool | `false` | Automatically connect to TikTok on game start. |
| General | `SpawnMode` | string | `Immediate` | When to spawn enemies: `Immediate` or `NightAware`. |
| General | `ShowOnScreenNotifications` | bool | `true` | Show on-screen popup when a gift is received. |
| Rules | `GiftRules` | string | | Map specific gift names to spawns. |
| Rules | `CoinRules` | string | | Map diamond totals to spawns (fallback when no gift name matches). |
| Rules | `LikeRules` | string | `100:E Melee:1` | Spawn an enemy every N likes. |
| Rules | `FollowRules` | string | `1:E Small Spider:1` | Spawn an enemy every N new stream followers. |

---

## Troubleshooting

**SmartScreen warning on the installer or Configurator**  
Click **More info** → **Run anyway**. Or right-click → **Properties** → tick **Unblock** → OK. This appears because the executables are not code-signed. The full source is available in this repo.

**Connection Error**  
Ensure your TikTok username is correct and your Live stream is currently active.

**Enemies not spawning**  
Check that you are in a level with active waves. In `Immediate` mode enemies spawn as soon as a gift is received — if you are on the main menu or between levels they will be queued until a level is active.

**Rules not applying**  
Verify the format in your rule fields. Ensure there are no trailing semicolons and that the enemy name matches exactly what is shown in the Configurator dropdown.
