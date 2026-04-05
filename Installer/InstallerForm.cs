using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

namespace ThronefallInteractiveInstaller
{
    public class InstallerForm : Form
    {
        // ── Controls ─────────────────────────────────────────────────────────────
        private TextBox    _gamePathBox;
        private Button     _browseBtn;
        private Label      _bepinexStatusLabel;
        private Label      _modStatusLabel;
        private Button     _installBtn;
        private ProgressBar _progressBar;
        private RichTextBox _logBox;
        private Button     _launchConfigBtn;

        // ── Constants ─────────────────────────────────────────────────────────────
        const string BepInExDownloadUrl =
            "https://thunderstore.io/package/download/BepInEx/BepInExPack_Thronefall/5.4.2100/";

        const string GitHubLatestReleaseUrl =
            "https://api.github.com/repos/RaisinRiotInteractive/Thronefall-Interactive-Mod/releases/latest";

        private static readonly HttpClient Http = new HttpClient();

        // ── Constructor ───────────────────────────────────────────────────────────
        public InstallerForm()
        {
            Http.DefaultRequestHeaders.UserAgent.ParseAdd("ThronefallInteractiveInstaller/1.0");
            BuildUI();
            SetIconFromExe();
            DetectAndSetGamePath();
        }

        void SetIconFromExe()
        {
            try
            {
                // Extract the icon embedded in the exe itself so the form title bar matches
                var exeIcon = Icon.ExtractAssociatedIcon(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
                if (exeIcon != null) Icon = exeIcon;
            }
            catch { }
        }

        // ── UI Construction ───────────────────────────────────────────────────────
        void BuildUI()
        {
            Text            = "ThronefallInteractive Installer";
            Width           = 620;
            Height          = 560;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox     = false;
            StartPosition   = FormStartPosition.CenterScreen;
            BackColor       = Color.FromArgb(30, 30, 30);
            ForeColor       = Color.White;
            Font            = new Font("Segoe UI", 9f);

            int y = 16;

            // ── Title ────────────────────────────────────────────────────────────
            var title = new Label
            {
                Text      = "ThronefallInteractive — Installer",
                Font      = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = Color.FromArgb(200, 170, 100),
                Left      = 16, Top = y,
                Width     = 580, Height = 30,
            };
            Controls.Add(title);
            y += 40;

            var subtitle = new Label
            {
                Text      = "Automatically installs BepInEx and the mod into your Thronefall folder.",
                ForeColor = Color.Silver,
                Left      = 16, Top = y, Width = 580, Height = 20,
            };
            Controls.Add(subtitle);
            y += 34;

            // ── Game path ─────────────────────────────────────────────────────────
            Controls.Add(MakeLabel("Thronefall game folder:", 16, y));
            y += 20;

            _gamePathBox = new TextBox
            {
                Left      = 16, Top = y,
                Width     = 494, Height = 24,
                BackColor = Color.FromArgb(50, 50, 50),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
            };
            _gamePathBox.TextChanged += (s, e) => RefreshStatus();
            Controls.Add(_gamePathBox);

            _browseBtn = new Button
            {
                Text      = "Browse...",
                Left      = 518, Top = y,
                Width     = 76, Height = 24,
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
            };
            _browseBtn.FlatAppearance.BorderColor = Color.FromArgb(90, 90, 90);
            _browseBtn.Click += OnBrowse;
            Controls.Add(_browseBtn);
            y += 36;

            // ── Status indicators ─────────────────────────────────────────────────
            _bepinexStatusLabel = MakeLabel("", 16, y);
            Controls.Add(_bepinexStatusLabel);
            y += 22;

            _modStatusLabel = MakeLabel("", 16, y);
            Controls.Add(_modStatusLabel);
            y += 30;

            // ── Install button ────────────────────────────────────────────────────
            _installBtn = new Button
            {
                Text      = "Install",
                Left      = 16, Top = y,
                Width     = 120, Height = 32,
                BackColor = Color.FromArgb(80, 160, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
                Enabled   = false,
            };
            _installBtn.FlatAppearance.BorderColor = Color.FromArgb(60, 130, 60);
            _installBtn.Click += async (s, e) => await RunInstall();
            Controls.Add(_installBtn);
            y += 44;

            // ── Progress bar ──────────────────────────────────────────────────────
            _progressBar = new ProgressBar
            {
                Left    = 16, Top = y,
                Width   = 578, Height = 16,
                Style   = ProgressBarStyle.Continuous,
                Visible = false,
            };
            Controls.Add(_progressBar);
            y += 26;

            // ── Log ───────────────────────────────────────────────────────────────
            Controls.Add(MakeLabel("Log:", 16, y));
            y += 20;

            _logBox = new RichTextBox
            {
                Left      = 16, Top = y,
                Width     = 578,
                Height    = ClientSize.Height - y - 52,
                BackColor = Color.FromArgb(20, 20, 20),
                ForeColor = Color.LightGray,
                ReadOnly  = true,
                BorderStyle = BorderStyle.None,
                Font      = new Font("Consolas", 8.5f),
                ScrollBars = RichTextBoxScrollBars.Vertical,
            };
            Controls.Add(_logBox);

            // ── Launch Configurator button (hidden until install complete) ─────────
            _launchConfigBtn = new Button
            {
                Text      = "Launch Configurator",
                Left      = 150, Top = ClientSize.Height - 44,
                Width     = 180, Height = 32,
                BackColor = Color.FromArgb(60, 120, 200),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
                Visible   = false,
                Anchor    = AnchorStyles.Bottom | AnchorStyles.Left,
            };
            _launchConfigBtn.FlatAppearance.BorderColor = Color.FromArgb(40, 90, 160);
            _launchConfigBtn.Click += OnLaunchConfigurator;
            Controls.Add(_launchConfigBtn);
        }

        Label MakeLabel(string text, int x, int y) => new Label
        {
            Text      = text,
            Left      = x, Top = y,
            Width     = 560, Height = 18,
            ForeColor = Color.Silver,
        };

        // ── Game path detection ───────────────────────────────────────────────────
        void DetectAndSetGamePath()
        {
            foreach (var path in GetThronefallBasePaths())
            {
                if (File.Exists(Path.Combine(path, "Thronefall.exe")))
                {
                    _gamePathBox.Text = path;
                    return;
                }
            }
            Log("Could not auto-detect Thronefall. Please browse to your game folder.", Color.Orange);
        }

        List<string> GetThronefallBasePaths()
        {
            var bases = new List<string>();

            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
                if (key?.GetValue("SteamPath") is string steamPath)
                    bases.Add(Path.Combine(steamPath, "steamapps", "common", "Thronefall"));
            }
            catch { }

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
                    bases.Add(Path.Combine(drive.RootDirectory.FullName, rel));
            }

            return bases;
        }

        void RefreshStatus()
        {
            string path = _gamePathBox.Text.Trim();
            bool validGame = File.Exists(Path.Combine(path, "Thronefall.exe"));

            if (!validGame)
            {
                _bepinexStatusLabel.Text      = "";
                _modStatusLabel.Text          = "";
                _installBtn.Enabled           = false;
                return;
            }

            bool hasBepInEx = Directory.Exists(Path.Combine(path, "BepInEx", "core"));
            bool hasMod     = File.Exists(Path.Combine(path, "BepInEx", "plugins",
                                "ThronefallInteractive", "ThronefallInteractive.dll"));

            SetStatus(_bepinexStatusLabel,
                hasBepInEx ? "BepInEx detected" : "BepInEx not found — will be downloaded and installed",
                hasBepInEx);

            SetStatus(_modStatusLabel,
                hasMod ? "ThronefallInteractive mod detected — will be updated"
                       : "ThronefallInteractive mod not found — will be installed",
                hasMod);

            _installBtn.Enabled = true;
            _installBtn.Text    = hasMod ? "Update" : "Install";
        }

        void SetStatus(Label label, string text, bool good)
        {
            label.ForeColor = good ? Color.FromArgb(100, 200, 100) : Color.FromArgb(220, 180, 80);
            label.Text      = (good ? "✔  " : "⚠  ") + text;
        }

        // ── Browse ────────────────────────────────────────────────────────────────
        void OnBrowse(object sender, EventArgs e)
        {
            using var dlg = new FolderBrowserDialog
            {
                Description         = "Select your Thronefall game folder (the one containing Thronefall.exe)",
                UseDescriptionForTitle = true,
            };
            if (dlg.ShowDialog() == DialogResult.OK)
                _gamePathBox.Text = dlg.SelectedPath;
        }

        // ── Install ───────────────────────────────────────────────────────────────
        async Task RunInstall()
        {
            string gamePath = _gamePathBox.Text.Trim();

            if (!File.Exists(Path.Combine(gamePath, "Thronefall.exe")))
            {
                MessageBox.Show("Thronefall.exe not found in the selected folder.\nPlease select the correct game folder.",
                    "Invalid Path", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SetInstalling(true);
            _logBox.Clear();

            try
            {
                // ── Step 1: BepInEx ───────────────────────────────────────────────
                bool hasBepInEx = Directory.Exists(Path.Combine(gamePath, "BepInEx", "core"));
                if (!hasBepInEx)
                {
                    Log("Downloading BepInEx...");
                    byte[] bepInExZip = await DownloadWithProgress(BepInExDownloadUrl, 0, 40);
                    Log("Extracting BepInEx...");
                    ExtractZipToFolder(bepInExZip, gamePath);
                    Log("BepInEx installed.", Color.FromArgb(100, 200, 100));
                }
                else
                {
                    Log("BepInEx already installed, skipping.");
                    SetProgress(40);
                }

                // ── Step 2: Mod ───────────────────────────────────────────────────
                Log("Fetching latest mod release from GitHub...");
                string modZipUrl = await GetLatestModDownloadUrl();
                Log($"Downloading mod: {modZipUrl.Split('/').Last()}...");
                byte[] modZip = await DownloadWithProgress(modZipUrl, 40, 90);

                Log("Extracting mod files...");
                ExtractZipToFolder(modZip, gamePath);
                SetProgress(100);

                Log("Installation complete!", Color.FromArgb(100, 200, 100));
                _launchConfigBtn.Visible = true;
                RefreshStatus();
            }
            catch (Exception ex)
            {
                Log($"Error: {ex.Message}", Color.FromArgb(220, 80, 80));
            }
            finally
            {
                SetInstalling(false);
            }
        }

        async Task<string> GetLatestModDownloadUrl()
        {
            string json = await Http.GetStringAsync(GitHubLatestReleaseUrl);
            using var doc = JsonDocument.Parse(json);
            var assets = doc.RootElement.GetProperty("assets");
            foreach (var asset in assets.EnumerateArray())
            {
                string name = asset.GetProperty("name").GetString() ?? "";
                if (name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                    return asset.GetProperty("browser_download_url").GetString();
            }
            throw new Exception("Could not find mod zip in latest GitHub release.");
        }

        async Task<byte[]> DownloadWithProgress(string url, int progressStart, int progressEnd)
        {
            using var response = await Http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            long? total = response.Content.Headers.ContentLength;
            using var stream = await response.Content.ReadAsStreamAsync();
            using var ms     = new MemoryStream();

            byte[] buffer = new byte[81920];
            long   read   = 0;
            int    bytes;

            while ((bytes = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                ms.Write(buffer, 0, bytes);
                read += bytes;

                if (total.HasValue)
                {
                    double fraction = (double)read / total.Value;
                    int progress = progressStart + (int)(fraction * (progressEnd - progressStart));
                    SetProgress(progress);
                }
            }

            return ms.ToArray();
        }

        // Thunderstore metadata files that should never be extracted to the game folder
        static readonly HashSet<string> ThunderstoreMetaFiles = new(StringComparer.OrdinalIgnoreCase)
        {
            "manifest.json", "icon.png", "README.md", "CHANGELOG.md", "SETUP.md",
            "ThronefallInteractiveInstaller.exe"
        };

        void ExtractZipToFolder(byte[] zipBytes, string destinationFolder)
        {
            using var ms      = new MemoryStream(zipBytes);
            using var archive = new ZipArchive(ms, ZipArchiveMode.Read);

            // Detect a common top-level folder prefix (e.g. "BepInExPack/") and strip it.
            // This handles Thunderstore packages that wrap everything in a subfolder.
            string stripPrefix = DetectStripPrefix(archive);

            foreach (var entry in archive.Entries)
            {
                string relativePath = entry.FullName;

                // Strip common prefix if present
                if (!string.IsNullOrEmpty(stripPrefix) &&
                    relativePath.StartsWith(stripPrefix, StringComparison.OrdinalIgnoreCase))
                    relativePath = relativePath.Substring(stripPrefix.Length);

                // Skip Thunderstore metadata files (at any level after stripping)
                if (ThunderstoreMetaFiles.Contains(relativePath) ||
                    ThunderstoreMetaFiles.Contains(entry.Name))
                    continue;

                // Skip empty paths (was just the prefix folder itself)
                if (string.IsNullOrEmpty(relativePath)) continue;

                string destPath = Path.Combine(destinationFolder, relativePath.Replace('/', Path.DirectorySeparatorChar));

                // Directory entry
                if (string.IsNullOrEmpty(entry.Name))
                {
                    Directory.CreateDirectory(destPath);
                    continue;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(destPath));
                using var entryStream = entry.Open();
                using var fileStream  = File.Create(destPath);
                entryStream.CopyTo(fileStream);

                Log($"  → {relativePath}");
            }
        }

        string DetectStripPrefix(ZipArchive archive)
        {
            // If every non-metadata entry shares the same top-level folder, strip it.
            string commonPrefix = null;
            foreach (var entry in archive.Entries)
            {
                if (ThunderstoreMetaFiles.Contains(entry.FullName)) continue;
                int slash = entry.FullName.IndexOf('/');
                if (slash < 0) return ""; // file at root — no prefix
                string prefix = entry.FullName.Substring(0, slash + 1);
                if (commonPrefix == null) commonPrefix = prefix;
                else if (!commonPrefix.Equals(prefix, StringComparison.OrdinalIgnoreCase)) return "";
            }
            return commonPrefix ?? "";
        }

        // ── Launch Configurator ───────────────────────────────────────────────────
        void OnLaunchConfigurator(object sender, EventArgs e)
        {
            string gamePath    = _gamePathBox.Text.Trim();
            string configExe   = Path.Combine(gamePath, "BepInEx", "plugins",
                                    "ThronefallInteractive", "ThronefallInteractiveConfigurator.exe");

            if (File.Exists(configExe))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName        = configExe,
                    UseShellExecute = true,
                });
            }
            else
            {
                MessageBox.Show("Configurator not found. Installation may not have completed successfully.",
                    "Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────────
        void SetInstalling(bool installing)
        {
            _installBtn.Enabled     = !installing;
            _browseBtn.Enabled      = !installing;
            _gamePathBox.Enabled    = !installing;
            _progressBar.Visible    = installing || _progressBar.Value > 0;
        }

        void SetProgress(int value)
        {
            if (InvokeRequired) { Invoke(() => SetProgress(value)); return; }
            _progressBar.Visible = true;
            _progressBar.Value   = Math.Clamp(value, 0, 100);
        }

        void Log(string message, Color? color = null)
        {
            if (InvokeRequired) { Invoke(() => Log(message, color)); return; }
            _logBox.SelectionStart  = _logBox.TextLength;
            _logBox.SelectionLength = 0;
            _logBox.SelectionColor  = color ?? Color.LightGray;
            _logBox.AppendText(message + "\n");
            _logBox.ScrollToCaret();
        }
    }
}
