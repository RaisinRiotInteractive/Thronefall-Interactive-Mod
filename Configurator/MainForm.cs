using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TikTokGiftsConfigurator
{
    public class MainForm : Form
    {
        // ── Controls ────────────────────────────────────────────────────────────
        private TextBox      _configPathBox;
        private TextBox      _usernameBox;
        private DataGridView _giftGrid;
        private DataGridView _coinGrid;
        private DataGridView _likeGrid;
        private DataGridView _followGrid;
        private Button       _enemyListBtn;
        private Label        _giftStatusLabel;
        private CheckBox     _notifCheck;
        private ComboBox     _spawnModeCombo;

        // ── State ───────────────────────────────────────────────────────────────
        private string _configPath = "";
        private List<string> _enemyNames    = new List<string>();   // from spawns JSON
        private List<string> _tiktokGifts   = new List<string>();   // fetched from TikTok
        private static readonly string[] SpawnModeValues = { "NightAware", "Immediate", "Queue" };

        // ── Constructor ─────────────────────────────────────────────────────────
        public MainForm()
        {
            Text          = "Thronefall Interactive Mod — Setup";
            Size          = new Size(800, 700);
            MinimumSize   = new Size(700, 600);
            StartPosition = FormStartPosition.CenterScreen;
            Font          = new Font("Segoe UI", 9f);
            BackColor     = Color.FromArgb(30, 30, 30);
            ForeColor     = Color.WhiteSmoke;

            BuildUI();
            AutoDetectConfig();
            _ = FetchTikTokGiftsAsync();
        }

        // ── UI Construction ─────────────────────────────────────────────────────
        void BuildUI()
        {
            var root = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                ColumnCount = 1,
                RowCount    = 8,
                Padding     = new Padding(10),
                BackColor   = Color.Transparent
            };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));    // config path
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));    // monster hint
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 25)); // gift rules
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 25)); // coin rules
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 25)); // like rules
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 25)); // follow rules
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));    // misc
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));    // buttons
            Controls.Add(root);

            // ── Row 0: Config file path ────────────────────────────────────────
            var pathPanel = new FlowLayoutPanel
            {
                AutoSize      = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents  = false,
                Dock          = DockStyle.Fill
            };
            pathPanel.Controls.Add(MakeLabel("Config file:"));
            _configPathBox = new TextBox
            {
                Width       = 420,
                BackColor   = Color.FromArgb(50, 50, 50),
                ForeColor   = Color.WhiteSmoke,
                BorderStyle = BorderStyle.FixedSingle
            };
            pathPanel.Controls.Add(_configPathBox);
            pathPanel.Controls.Add(MakeButton("Browse", OnBrowse));
            pathPanel.Controls.Add(MakeButton("Load",   OnLoad));
            root.Controls.Add(pathPanel, 0, 0);

            // ── Row 1: Enemy list button ───────────────────────────────────────
            _enemyListBtn = MakeButton("View Available Enemies", OnShowEnemyList);
            _enemyListBtn.Margin = new Padding(0, 4, 0, 4);
            root.Controls.Add(_enemyListBtn, 0, 1);

            // ── Row 2: Gift Rules ──────────────────────────────────────────────
            var giftGroup = MakeGroupBox("Gift Rules  —  TikTok gift name → enemy  (GiftName : Enemy : Count)");
            _giftGrid = BuildGrid(new[] { "Gift Name (type to autocomplete)", "Enemy", "Count" });

            // Autocomplete gift names as the user types in column 0
            _giftGrid.EditingControlShowing += OnGiftGridEditingControlShowing;

            giftGroup.Controls.Add(_giftGrid);

            // Status bar at the bottom of the gift group (above the add-row button)
            var giftStatusPanel = new FlowLayoutPanel
            {
                Dock          = DockStyle.Bottom,
                AutoSize      = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents  = false,
                BackColor     = Color.Transparent,
                Padding       = new Padding(0, 2, 0, 0)
            };
            _giftStatusLabel = new Label
            {
                AutoSize  = true,
                ForeColor = Color.FromArgb(120, 180, 120),
                Text      = "Fetching TikTok gift list...",
                TextAlign = ContentAlignment.MiddleLeft,
                Margin    = new Padding(0, 3, 8, 0)
            };
            var refreshBtn = MakeButton("↻ Refresh Gifts", async (s, e) => await FetchTikTokGiftsAsync());
            giftStatusPanel.Controls.Add(_giftStatusLabel);
            giftStatusPanel.Controls.Add(refreshBtn);
            giftGroup.Controls.Add(giftStatusPanel);

            var giftAdd = MakeButton("+ Add Row", (s, e) => AddRow(_giftGrid, new object[] { "", "", "1" }));
            giftAdd.Dock = DockStyle.Bottom;
            giftGroup.Controls.Add(giftAdd);
            root.Controls.Add(giftGroup, 0, 2);

            // ── Row 3: Coin Rules ──────────────────────────────────────────────
            var coinGroup = MakeGroupBox("Coin Rules  —  diamond total → enemy  (MinDiamonds : Enemy : Count)  highest match wins");
            _coinGrid = BuildGrid(new[] { "Min Diamonds", "Enemy", "Count" });
            coinGroup.Controls.Add(_coinGrid);
            var coinAdd = MakeButton("+ Add Row", (s, e) => AddRow(_coinGrid, new object[] { "5", "", "1" }));
            coinAdd.Dock = DockStyle.Bottom;
            coinGroup.Controls.Add(coinAdd);
            root.Controls.Add(coinGroup, 0, 3);

            // ── Row 4: Like Rules ──────────────────────────────────────────────
            var likeGroup = MakeGroupBox("Like Rules  —  likes per spawn → enemy  (Likes : Enemy : Count)");
            _likeGrid = BuildGrid(new[] { "Likes per Spawn", "Enemy", "Count" });
            likeGroup.Controls.Add(_likeGrid);
            var likeAdd = MakeButton("+ Add Row", (s, e) => AddRow(_likeGrid, new object[] { "100", "", "1" }));
            likeAdd.Dock = DockStyle.Bottom;
            likeGroup.Controls.Add(likeAdd);
            root.Controls.Add(likeGroup, 0, 4);

            // ── Row 5: Follow Rules ───────────────────────────────────────────
            var followGroup = MakeGroupBox("Follow Rules  —  follows per spawn → enemy  (Follows : Enemy : Count)");
            _followGrid = BuildGrid(new[] { "Follows per Spawn", "Enemy", "Count" });
            followGroup.Controls.Add(_followGrid);
            var followAdd = MakeButton("+ Add Row", (s, e) => AddRow(_followGrid, new object[] { "1", "", "1" }));
            followAdd.Dock = DockStyle.Bottom;
            followGroup.Controls.Add(followAdd);
            root.Controls.Add(followGroup, 0, 5);

            // ── Row 6: Misc ────────────────────────────────────────────────────
            var miscPanel = new FlowLayoutPanel { AutoSize = true, BackColor = Color.Transparent, WrapContents = true };
            var usernamePanel = new FlowLayoutPanel
            {
                AutoSize      = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents  = false,
                Dock          = DockStyle.Fill,
                Margin        = new Padding(0, 0, 0, 6)
            };
            usernamePanel.Controls.Add(MakeLabel("TikTok Username:"));
            _usernameBox = new TextBox
            {
                Width       = 200,
                BackColor   = Color.FromArgb(50, 50, 50),
                ForeColor   = Color.WhiteSmoke,
                BorderStyle = BorderStyle.FixedSingle,
                Margin      = new Padding(6, 2, 0, 0)
            };
            usernamePanel.Controls.Add(_usernameBox);
            miscPanel.Controls.Add(usernamePanel);
            miscPanel.SetFlowBreak(usernamePanel, true);

            _notifCheck = new CheckBox
            {
                Text      = "Show on-screen notifications",
                Checked   = true,
                ForeColor = Color.WhiteSmoke,
                Margin    = new Padding(0, 4, 20, 0)
            };
            miscPanel.Controls.Add(_notifCheck);
            miscPanel.Controls.Add(MakeLabel("Spawn Mode:"));
            _spawnModeCombo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor     = Color.FromArgb(50, 50, 50),
                ForeColor     = Color.WhiteSmoke,
                FlatStyle     = FlatStyle.Flat,
                Width         = 200,
                Margin        = new Padding(4, 2, 0, 0)
            };
            _spawnModeCombo.Items.AddRange(new object[] { "NightAware (default)", "Immediate", "Queue" });
            _spawnModeCombo.SelectedIndex = 0;
            miscPanel.Controls.Add(_spawnModeCombo);
            root.Controls.Add(miscPanel, 0, 6);

            // ── Row 7: Buttons ─────────────────────────────────────────────────
            var btnPanel = new FlowLayoutPanel { AutoSize = true, BackColor = Color.Transparent, FlowDirection = FlowDirection.RightToLeft };
            btnPanel.Controls.Add(MakeButton("Save & Close", OnSaveClose, accent: true));
            btnPanel.Controls.Add(MakeButton("Save",         (s, e) => OnSave(s, e), accent: true));
            root.Controls.Add(btnPanel, 0, 7);
        }

        // ── Grid builder ────────────────────────────────────────────────────────
        DataGridView BuildGrid(string[] headers)
        {
            var grid = new DataGridView
            {
                Dock                    = DockStyle.Fill,
                AllowUserToAddRows      = false,
                AllowUserToDeleteRows   = false,
                RowHeadersVisible       = false,
                MultiSelect             = false,
                SelectionMode           = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor         = Color.FromArgb(40, 40, 40),
                GridColor               = Color.FromArgb(70, 70, 70),
                DefaultCellStyle        = { BackColor = Color.FromArgb(45, 45, 45), ForeColor = Color.WhiteSmoke, SelectionBackColor = Color.FromArgb(70, 90, 120) },
                ColumnHeadersDefaultCellStyle = { BackColor = Color.FromArgb(55, 55, 55), ForeColor = Color.WhiteSmoke },
                EnableHeadersVisualStyles = false
            };

            // Col 0: key (text)
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText   = headers[0],
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight   = 40
            });

            // Col 1: enemy (combo)
            var enemyCol = new DataGridViewComboBoxColumn
            {
                HeaderText                  = headers[1],
                AutoSizeMode                = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight                  = 45,
                FlatStyle                   = FlatStyle.Flat,
                DisplayStyleForCurrentCellOnly = true
            };
            enemyCol.DefaultCellStyle.BackColor = Color.FromArgb(45, 45, 45);
            enemyCol.DefaultCellStyle.ForeColor = Color.WhiteSmoke;
            grid.Columns.Add(enemyCol);

            // Col 2: count (text, narrow)
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText   = headers[2],
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight   = 15
            });

            // Col 3: delete button
            grid.Columns.Add(new DataGridViewButtonColumn
            {
                HeaderText                  = "",
                Text                        = "✕",
                UseColumnTextForButtonValue = true,
                Width                       = 30,
                AutoSizeMode                = DataGridViewAutoSizeColumnMode.None
            });

            grid.CellClick += (s, e) =>
            {
                if (e.ColumnIndex == grid.Columns.Count - 1 && e.RowIndex >= 0)
                    grid.Rows.RemoveAt(e.RowIndex);
            };

            // Delete key removes selected row
            grid.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Delete && grid.CurrentRow != null)
                    grid.Rows.Remove(grid.CurrentRow);
            };

            // Suppress validation errors for combo items not in list (handles loaded values)
            grid.DataError += (s, e) => e.Cancel = true;

            return grid;
        }

        // ── Populate enemy combo columns ─────────────────────────────────────────
        void RefreshEnemyColumns()
        {
            SetEnemyComboItems(_giftGrid);
            SetEnemyComboItems(_coinGrid);
            SetEnemyComboItems(_likeGrid);
            SetEnemyComboItems(_followGrid);
        }

        void SetEnemyComboItems(DataGridView grid)
        {
            if (grid.Columns.Count < 2) return;
            var col = grid.Columns[1] as DataGridViewComboBoxColumn;
            if (col == null) return;

            col.Items.Clear();
            foreach (var name in _enemyNames)
                col.Items.Add(name);
        }

        void AddRow(DataGridView grid, object[] values)
        {
            int idx = grid.Rows.Add();
            var row = grid.Rows[idx];
            row.Cells[0].Value = values.Length > 0 ? values[0] : "";
            row.Cells[2].Value = values.Length > 2 ? values[2] : "1";

            // Enemy combo: set first available item as default
            var col = grid.Columns[1] as DataGridViewComboBoxColumn;
            if (col != null && col.Items.Count > 0)
                row.Cells[1].Value = values.Length > 1 && col.Items.Contains(values[1])
                    ? values[1]
                    : col.Items[0];
            else
                row.Cells[1].Value = values.Length > 1 ? values[1] : "";
        }

        void PopulateGrid(DataGridView grid, string raw)
        {
            grid.Rows.Clear();
            if (string.IsNullOrWhiteSpace(raw)) return;

            var col = grid.Columns[1] as DataGridViewComboBoxColumn;

            foreach (var part in raw.Split(';'))
            {
                var seg = part.Trim().Split(':');
                if (seg.Length != 3) continue;

                string key    = seg[0].Trim();
                string prefab = seg[1].Trim();
                string count  = seg[2].Trim();

                // Ensure the saved prefab name is in the combo list even if not in the JSON yet
                if (col != null && !string.IsNullOrEmpty(prefab) && !col.Items.Contains(prefab))
                    col.Items.Add(prefab);

                int rowIdx = grid.Rows.Add();
                grid.Rows[rowIdx].Cells[0].Value = key;
                grid.Rows[rowIdx].Cells[1].Value = prefab;
                grid.Rows[rowIdx].Cells[2].Value = count;
            }
        }

        string SerialiseGrid(DataGridView grid)
        {
            var parts = new List<string>();
            foreach (DataGridViewRow row in grid.Rows)
            {
                var key    = row.Cells[0].Value?.ToString()?.Trim() ?? "";
                var prefab = row.Cells[1].Value?.ToString()?.Trim() ?? "";
                var count  = row.Cells[2].Value?.ToString()?.Trim() ?? "";
                if (key.Length > 0 && prefab.Length > 0 && count.Length > 0)
                    parts.Add($"{key}:{prefab}:{count}");
            }
            return string.Join(";", parts);
        }

        // ── TikTok gift list ─────────────────────────────────────────────────────
        // TikTok's gift API requires browser session cookies — it 403s from desktop apps.
        // We use a built-in list of all known TikTok gifts instead.
        Task FetchTikTokGiftsAsync()
        {
            _tiktokGifts = new List<string>
            {
                "Aurora", "Baby Panda", "Basketball", "BBQ Grill", "Bear",
                "Birthday Cake", "Blue Dragon", "Bro", "Butterfly",
                "Cap", "Cake Slice", "Cheer Up", "Cherry", "Concert",
                "Confetti", "Cookie", "Corgi", "Crown",
                "Diamond", "Dolphin", "Doughnut", "Drama Queen",
                "Elephant", "Eye Glasses",
                "Fingers Heart", "Finger Heart", "Firestorm", "Flame", "Flame Heart",
                "Flying Fish", "Football", "Friendship Necklace", "Fury",
                "GG", "Galaxy", "Garlic Bread", "Ghost",
                "Hand Heart", "Hat and Mustache", "Hearts", "Heart Me",
                "Ice Cream Cone", "I Love You", "Italian Hand",
                "Kiss",
                "Lightning", "Lion", "Little Crown", "Love Bang", "Lovely", "Lucky Cat",
                "Magic Wand", "Mic", "Money Gun", "Music Play",
                "Owl",
                "Panda", "Paper Crane", "Party Popper", "Perfume", "Pew Pew Pew",
                "Pinwheel", "Pink Flower", "Potato", "Power Gem",
                "Rainbow Puke", "Rocket", "Rose", "Rose Bouquet",
                "Shark", "Star", "Sun Cream", "Sunglasses", "Sushi",
                "Taco", "Thunder Angel", "Ticket", "TikTok", "Tiny Diny", "Trending",
                "UFO", "Universe",
                "Vintage Vase",
                "Watermelon", "Whale",
            };
            _tiktokGifts.Sort(StringComparer.OrdinalIgnoreCase);

            SetGiftStatus($"Built-in gift list ready ({_tiktokGifts.Count} gifts)  —  type to autocomplete.",
                          Color.FromArgb(120, 200, 120));
            return Task.CompletedTask;
        }

        void SetGiftStatus(string text, Color color)
        {
            if (_giftStatusLabel == null) return;
            _giftStatusLabel.Text      = text;
            _giftStatusLabel.ForeColor = color;
        }

        // ── Autocomplete for gift name column ────────────────────────────────────
        void OnGiftGridEditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            // Only apply autocomplete when editing column 0 (Gift Name)
            if (_giftGrid.CurrentCell?.ColumnIndex != 0 || e.Control is not TextBox tb)
            {
                if (e.Control is TextBox other) other.AutoCompleteMode = AutoCompleteMode.None;
                return;
            }

            // Re-build source each time so newly fetched gifts are always current
            var src = new AutoCompleteStringCollection();
            if (_tiktokGifts.Count > 0)
                src.AddRange(_tiktokGifts.ToArray());

            tb.AutoCompleteMode         = AutoCompleteMode.SuggestAppend;
            tb.AutoCompleteSource       = AutoCompleteSource.CustomSource;
            tb.AutoCompleteCustomSource = src;
        }

        // ── Auto-detect ──────────────────────────────────────────────────────────
        void AutoDetectConfig()
        {
            string cfgName = "com.raisinriotinteractive.thronefall.interactive.cfg";
            string tail    = Path.Combine("BepInEx", "config", cfgName);
            var paths      = new List<string>();

            // 1. Relative to EXE (lives in BepInEx/plugins/ → ../config/)
            paths.Add(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "config", cfgName)));

            // 2. Steam registry — primary library
            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
                if (key?.GetValue("SteamPath") is string steamPath)
                    paths.Add(Path.Combine(steamPath, "steamapps", "common", "Thronefall", tail));
            }
            catch { }

            // 3. Scan every available drive for common Steam install patterns
            var relPaths = new[]
            {
                @"SteamLibrary\steamapps\common\Thronefall",
                @"Steam\steamapps\common\Thronefall",
                @"Program Files (x86)\Steam\steamapps\common\Thronefall",
                @"Program Files\Steam\steamapps\common\Thronefall",
                @"Games\Steam\steamapps\common\Thronefall",
                @"Games\steamapps\common\Thronefall",
            };

            foreach (var drive in DriveInfo.GetDrives()
                         .Where(d => d.IsReady && (d.DriveType == DriveType.Fixed ||
                                                   d.DriveType == DriveType.Removable)))
            {
                foreach (var rel in relPaths)
                    paths.Add(Path.Combine(drive.RootDirectory.FullName, rel, tail));
            }

            foreach (var p in paths)
            {
                if (File.Exists(p))
                {
                    _configPathBox.Text = p;
                    OnLoad(null, null);
                    return;
                }
            }
        }

        // ── Enemy list popup ─────────────────────────────────────────────────────
        void OnShowEnemyList(object s, EventArgs e)
        {
            var names = _enemyNames.Count > 0
                ? _enemyNames
                : new List<string> { "(No enemies loaded — load the config first)" };

            var win = new Form
            {
                Text          = "Available Enemies",
                Size          = new Size(340, 480),
                MinimumSize   = new Size(260, 300),
                StartPosition = FormStartPosition.CenterParent,
                Font          = new Font("Segoe UI", 9f),
                BackColor     = Color.FromArgb(30, 30, 30),
                ForeColor     = Color.WhiteSmoke
            };

            var note = new Label
            {
                Text      = "Click a name to copy it to the clipboard.",
                AutoSize  = true,
                ForeColor = Color.FromArgb(150, 200, 255),
                Dock      = DockStyle.Top,
                Padding   = new Padding(8, 6, 8, 4)
            };

            var scroll = new Panel
            {
                Dock       = DockStyle.Fill,
                AutoScroll = true,
                Padding    = new Padding(6)
            };

            var flow = new FlowLayoutPanel
            {
                AutoSize      = true,
                AutoSizeMode  = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.TopDown,
                WrapContents  = false,
                Dock          = DockStyle.Top,
                Padding       = new Padding(2)
            };

            foreach (var name in names)
            {
                var btn = new Button
                {
                    Text      = name,
                    AutoSize  = false,
                    Width     = 280,
                    Height    = 26,
                    BackColor = Color.FromArgb(55, 55, 55),
                    ForeColor = Color.WhiteSmoke,
                    FlatStyle = FlatStyle.Flat,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Padding   = new Padding(4, 0, 0, 0),
                    Margin    = new Padding(0, 0, 0, 3),
                    Tag       = name
                };
                btn.FlatAppearance.BorderColor = Color.FromArgb(70, 70, 70);
                btn.Click += (bs, be) =>
                {
                    Clipboard.SetText((string)((Button)bs).Tag);
                    btn.Text = $"✓ copied — {name}";
                    var t = new System.Windows.Forms.Timer { Interval = 1200 };
                    t.Tick += (ts, te) => { btn.Text = name; t.Stop(); t.Dispose(); };
                    t.Start();
                };
                flow.Controls.Add(btn);
            }

            scroll.Controls.Add(flow);
            win.Controls.Add(scroll);
            win.Controls.Add(note);
            win.ShowDialog(this);
        }

        // ── Load ─────────────────────────────────────────────────────────────────
        void OnBrowse(object s, EventArgs e)
        {
            using var dlg = new OpenFileDialog
            {
                Title  = "Select BepInEx config file",
                Filter = "Config files (*.cfg)|*.cfg|All files (*.*)|*.*"
            };
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                _configPathBox.Text = dlg.FileName;
                OnLoad(null, null);
            }
        }

        void OnLoad(object s, EventArgs e)
        {
            _configPath = _configPathBox.Text.Trim();
            if (!File.Exists(_configPath)) return;

            var cfg = ReadConfig(_configPath);

            // Load spawn info first so combo columns are ready
            LoadSpawnInfo(Path.Combine(Path.GetDirectoryName(_configPath), "interactive_spawns.json"));

            _usernameBox.Text = cfg.GetValueOrDefault("General.TikTokUsername", "");

            PopulateGrid(_giftGrid, cfg.GetValueOrDefault("Rules.GiftRules", ""));
            PopulateGrid(_coinGrid, cfg.GetValueOrDefault("Rules.CoinRules", ""));
            PopulateGrid(_likeGrid, cfg.GetValueOrDefault("Rules.LikeRules", ""));
            PopulateGrid(_followGrid, cfg.GetValueOrDefault("Rules.FollowRules", ""));

            _notifCheck.Checked = cfg.GetValueOrDefault("General.ShowOnScreenNotifications", "true")
                                     .Trim().Equals("true", StringComparison.OrdinalIgnoreCase);

            string mode   = cfg.GetValueOrDefault("General.SpawnMode", "NightAware");
            int    modeIdx = Array.FindIndex(SpawnModeValues, v => v.Equals(mode, StringComparison.OrdinalIgnoreCase));
            _spawnModeCombo.SelectedIndex = modeIdx < 0 ? 0 : modeIdx;
        }

        void LoadSpawnInfo(string jsonPath)
        {
            _enemyNames.Clear();

            if (!File.Exists(jsonPath))
            {
                _enemyListBtn.Text = "View Available Enemies  (load config first)";
                RefreshEnemyColumns();
                return;
            }

            try
            {
                var text = File.ReadAllText(jsonPath);

                // Pull the flat allEnemies array if present
                var flatMatch = System.Text.RegularExpressions.Regex.Match(
                    text, "\"allEnemies\"\\s*:\\s*\\[([^\\]]*?)\\]");

                if (flatMatch.Success)
                {
                    _enemyNames = flatMatch.Groups[1].Value
                        .Split(',')
                        .Select(n => n.Trim().Trim('"'))
                        .Where(n => n.Length > 0)
                        .Distinct()
                        .OrderBy(n => n)
                        .ToList();
                }
                else
                {
                    // Fall back: collect names from per-wave arrays
                    var waveMatches = System.Text.RegularExpressions.Regex.Matches(
                        text, "\"(\\d+)\"\\s*:\\s*\\[([^\\]]*?)\\]");

                    var names = new HashSet<string>();
                    foreach (System.Text.RegularExpressions.Match m in waveMatches)
                    {
                        foreach (var n in m.Groups[2].Value.Split(',')
                                     .Select(x => x.Trim().Trim('"'))
                                     .Where(x => x.Length > 0))
                            names.Add(n);
                    }
                    _enemyNames = names.OrderBy(n => n).ToList();
                }

                RefreshEnemyColumns();
                _enemyListBtn.Text = $"View Available Enemies  ({_enemyNames.Count})";
            }
            catch (Exception ex)
            {
                _enemyListBtn.Text = $"View Available Enemies  (parse error: {ex.Message})";
            }
        }

        // ── Save ─────────────────────────────────────────────────────────────────
        bool OnSave(object s, EventArgs e)
        {
            if (!File.Exists(_configPath))
            {
                MessageBox.Show("Config file not found. Use Browse to locate it.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            try
            {
                var cfg = ReadConfig(_configPath);
                cfg["General.TikTokUsername"]             = _usernameBox.Text.Trim();
                cfg["Rules.GiftRules"]                   = SerialiseGrid(_giftGrid);
                cfg["Rules.CoinRules"]                   = SerialiseGrid(_coinGrid);
                cfg["Rules.LikeRules"]                   = SerialiseGrid(_likeGrid);
                cfg["Rules.FollowRules"]                 = SerialiseGrid(_followGrid);
                cfg["General.ShowOnScreenNotifications"] = _notifCheck.Checked ? "true" : "false";
                cfg["General.SpawnMode"]                 = SpawnModeValues[Math.Max(0, _spawnModeCombo.SelectedIndex)];

                if (WriteConfig(_configPath, cfg))
                {
                    MessageBox.Show("Saved!", "Thronefall Interactive Mod", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return false;
        }

        void OnSaveClose(object s, EventArgs e) { if (OnSave(s, e)) Close(); }

        // ── BepInEx config parser ────────────────────────────────────────────────
        Dictionary<string, string> ReadConfig(string path)
        {
            var dict    = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string section = "";
            foreach (var rawLine in File.ReadAllLines(path))
            {
                var line = rawLine.Trim();
                if (line.StartsWith("[") && line.EndsWith("]")) { section = line[1..^1].Trim(); continue; }
                if (line.StartsWith("#") || line.StartsWith(";") || line.Length == 0) continue;
                int eq = line.IndexOf('=');
                if (eq < 0) continue;
                dict[$"{section}.{line[..eq].Trim()}"] = line[(eq + 1)..].Trim();
            }
            return dict;
        }

        bool WriteConfig(string path, Dictionary<string, string> updated)
        {
            try
            {
                var lines   = File.ReadAllLines(path).ToList();
                string sect = "";
                var written = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                for (int i = 0; i < lines.Count; i++)
                {
                    var line = lines[i].Trim();
                    if (line.StartsWith("[") && line.EndsWith("]")) { sect = line[1..^1].Trim(); continue; }
                    int eq = line.IndexOf('=');
                    if (eq < 0) continue;
                    var fullKey = $"{sect}.{line[..eq].Trim()}";
                    if (updated.TryGetValue(fullKey, out string newVal))
                    {
                        lines[i] = $"{line[..eq].Trim()} = {newVal}";
                        written.Add(fullKey);
                    }
                }

                foreach (var grp in updated.Where(kv => !written.Contains(kv.Key))
                                           .GroupBy(kv => kv.Key.Split('.')[0]))
                {
                    lines.Add($"[{grp.Key}]");
                    foreach (var kv in grp)
                        lines.Add($"{kv.Key[(kv.Key.IndexOf('.') + 1)..]} = {kv.Value}");
                }

                File.WriteAllLines(path, lines);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"File error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        // ── Layout helpers ────────────────────────────────────────────────────────
        Label MakeLabel(string text) => new Label
        {
            Text      = text,
            AutoSize  = true,
            ForeColor = Color.WhiteSmoke,
            TextAlign = ContentAlignment.MiddleLeft
        };

        Button MakeButton(string text, EventHandler onClick, bool accent = false)
        {
            var b = new Button
            {
                Text      = text,
                AutoSize  = true,
                BackColor = accent ? Color.FromArgb(40, 90, 160) : Color.FromArgb(60, 60, 60),
                ForeColor = Color.WhiteSmoke,
                FlatStyle = FlatStyle.Flat,
                Margin    = new Padding(4, 2, 4, 2)
            };
            b.FlatAppearance.BorderColor = Color.FromArgb(90, 90, 90);
            b.Click += onClick;
            return b;
        }

        GroupBox MakeGroupBox(string text) => new GroupBox
        {
            Text      = text,
            Dock      = DockStyle.Fill,
            ForeColor = Color.FromArgb(150, 200, 255),
            BackColor = Color.Transparent,
            Padding   = new Padding(6)
        };
    }
}
