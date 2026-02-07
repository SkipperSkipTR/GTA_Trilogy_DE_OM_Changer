using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;

namespace SA_DE_OM_Changer
{
    public partial class MainWindow : Window
    {
        private readonly DispatcherTimer _timer;
        private readonly MemorySession _mem = new();
        private GameAddressConfig _config;

        private IntPtr _baseAddress = IntPtr.Zero;
        private IntPtr _targetAddress = IntPtr.Zero;
        private long _offset;
        private string? _version;
        private GameInfo? _currentGame;

        private int _hotkeyId = 1;
        private const uint WM_HOTKEY = 0x0312;

        public MainWindow()
        {
            InitializeComponent();
            _config = new GameAddressConfig(AppContext.BaseDirectory);

            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _timer.Tick += (_, __) => Tick();
            _timer.Start();

            LoadHotkeyDisplay();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            RegisterCurrentHotkey();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            UnregisterCurrentHotkey();
            _mem.Detach();
        }

        private void RegisterCurrentHotkey()
        {
            var handle = new WindowInteropHelper(this).Handle;
            if (handle == IntPtr.Zero) return;

            HwndSource.FromHwnd(handle)?.AddHook(WndProc);

            var hotkey = _config.GetHotkey();
            Native.RegisterHotKey(handle, _hotkeyId, hotkey.Modifiers, hotkey.VirtualKey);
        }

        private void UnregisterCurrentHotkey()
        {
            var handle = new WindowInteropHelper(this).Handle;
            if (handle != IntPtr.Zero)
            {
                Native.UnregisterHotKey(handle, _hotkeyId);
            }
        }

        private void LoadHotkeyDisplay()
        {
            var hotkey = _config.GetHotkey();
            HotkeyText.Text = $"Current Hotkey: {hotkey.DisplayName}";
        }

        private void Tick()
        {
            if (!_mem.IsAttached)
            {
                // Check for any supported game process
                foreach (var game in _config.GetSupportedGames())
                {
                    var proc = Process.GetProcessesByName(game.ProcessName).FirstOrDefault();
                    if (proc != null)
                    {
                        _currentGame = game;
                        Attach(proc);
                        break;
                    }
                }
            }
            else
            {
                if (_mem.ProcessHasExited)
                {
                    Detach("Process exited.");
                    return;
                }

                if (_targetAddress != IntPtr.Zero && _mem.ReadByte(_targetAddress, out byte val))
                {
                    ValueText.Text = $"[{_currentGame?.DisplayName}] on_mission Value: {val}";
                    StatusText.Text = $"Status: Attached to {_currentGame?.DisplayName}. Use {_config.GetHotkey().DisplayName} to toggle.";
                    StatusText.Foreground = System.Windows.Media.Brushes.White;
                }
                else
                {
                    Detach("Failed to read value.");
                }
            }
        }

        private void Attach(Process proc)
        {
            if (!_mem.Attach(proc))
            {
                StatusText.Text = "Status: Failed to attach";
                StatusText.Foreground = System.Windows.Media.Brushes.OrangeRed;
                return;
            }

            try
            {
                _baseAddress = proc.MainModule?.BaseAddress ?? IntPtr.Zero;
                var exePath = proc.MainModule?.FileName ?? string.Empty;
                var verInfo = FileVersionInfo.GetVersionInfo(exePath);
                string fileVersion = string.Format("{0}.{1}.{2}.{3}", verInfo.FileMajorPart,
                                                      verInfo.FileMinorPart,
                                                      verInfo.FileBuildPart,
                                                      verInfo.FilePrivatePart);
                _version = fileVersion;

                if (_currentGame == null || !_config.TryGetOffset(_currentGame.ProcessName, _version, out _offset))
                {
                    Detach($"Unsupported version for {_currentGame?.DisplayName} (add via button).");
                    return;
                }

                _targetAddress = new IntPtr(_baseAddress.ToInt64() + _offset);
                StatusText.Text = $"Status: Attached to {_currentGame.DisplayName}. Use {_config.GetHotkey().DisplayName} to toggle.";
                StatusText.Foreground = System.Windows.Media.Brushes.White;
            }
            catch (Exception ex)
            {
                Detach($"Attach error: {ex.Message}");
            }
        }

        private void Detach(string reason)
        {
            _mem.Detach();
            _baseAddress = IntPtr.Zero;
            _targetAddress = IntPtr.Zero;
            _currentGame = null;
            StatusText.Text = $"Status: {reason}";
            StatusText.Foreground = System.Windows.Media.Brushes.Red;
            ValueText.Text = "on_mission Value: —";
        }

        private void Toggle()
        {
            if (!_mem.IsAttached || _targetAddress == IntPtr.Zero) return;
            if (_mem.ReadByte(_targetAddress, out byte current))
            {
                byte normalized = current != 0 ? (byte)1 : (byte)0;
                byte next = normalized == 0 ? (byte)1 : (byte)0;
                _mem.WriteByte(_targetAddress, next);
                ValueText.Text = $"[{_currentGame?.DisplayName}] on_mission Value: {next}";
            }
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY && wParam.ToInt32() == _hotkeyId)
            {
                Toggle();
                handled = true;
            }
            return IntPtr.Zero;
        }

        private void AddVersion_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new AddVersionWindow(_config.GetSupportedGames()) { Owner = this };
            if (dlg.ShowDialog() == true)
            {
                if (_config.AddAdditionalVersion(dlg.SelectedGame, dlg.GameVersion, dlg.AddressHex))
                {
                    MessageBox.Show($"Version {dlg.GameVersion} added for {dlg.SelectedGame}.", "Success",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Failed to add version (maybe already exists).", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ChangeHotkey_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new HotkeyConfigWindow(_config.GetHotkey()) { Owner = this };
            if (dlg.ShowDialog() == true)
            {
                UnregisterCurrentHotkey();
                _config.SaveHotkey(dlg.SelectedHotkey);
                RegisterCurrentHotkey();
                LoadHotkeyDisplay();

                MessageBox.Show($"Hotkey changed to {dlg.SelectedHotkey.DisplayName}", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }

    public sealed class MemorySession
    {
        private IntPtr _handle = IntPtr.Zero;
        private Process? _proc;

        public bool IsAttached => _handle != IntPtr.Zero;
        public bool ProcessHasExited => _proc == null || _proc.HasExited;

        public bool Attach(Process proc)
        {
            Detach();
            _proc = proc;
            _handle = Native.OpenProcess(Native.ProcessAccessFlags.All, false, proc.Id);
            return _handle != IntPtr.Zero;
        }

        public void Detach()
        {
            if (_handle != IntPtr.Zero)
            {
                Native.CloseHandle(_handle);
                _handle = IntPtr.Zero;
            }
            _proc = null;
        }

        public bool ReadByte(IntPtr addr, out byte val)
        {
            val = 0;
            var buffer = new byte[1];
            if (!IsAttached) return false;
            if (Native.ReadProcessMemory(_handle, addr, buffer, buffer.Length, out _))
            {
                val = buffer[0];
                return true;
            }
            return false;
        }

        public bool WriteByte(IntPtr addr, byte val)
        {
            if (!IsAttached) return false;
            var buffer = new[] { val };
            return Native.WriteProcessMemory(_handle, addr, buffer, buffer.Length, out _);
        }
    }

    public class GameInfo
    {
        public string ProcessName { get; set; } = "";
        public string DisplayName { get; set; } = "";
    }

    public class HotkeyInfo
    {
        public uint VirtualKey { get; set; }
        public uint Modifiers { get; set; }
        public string DisplayName { get; set; } = "";
    }

    public sealed class GameAddressConfig
    {
        // Hardcoded offsets for each game
        private readonly Dictionary<string, Dictionary<string, string>> _hardcodedOffsets = new(StringComparer.OrdinalIgnoreCase)
        {
            {
                "SanAndreas", new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    {"1.0.0.14296", "500CD78"}, // Base Version 1.0
                    {"1.0.0.14388", "5010878"}, // Title Update 1.01
                    {"1.0.0.14718", "501CB78"}, // Title Update 1.03
                    {"1.0.0.15483", "501E838"}, // Title Update 1.04
                    {"1.0.8.11827", "5095E08"}, // Title Update 1.04.5
                    {"1.0.17.38838", "513003C"}, // Steam Release
                    {"1.0.17.39540", "5137698"}, // Epic Release
                    {"1.0.112.6680", "51BE148"}, // Title Update 1.112
                    {"1.0.113.21181", "51BF148"}, // Steam Only 1.113 Update
                }
            },
            {
                "ViceCity", new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    {"1.0.0.14296", "0x4E68394"}, // Base Version 1.0
                    {"1.0.0.14388", "0x4E6F794"}, // Title Update 1.01
                    {"1.0.0.14718", "0x4E74B14"}, // Title Update 1.03
                    {"1.0.0.15399", "0x4E61E74"}, // Title Update 1.04
                    {"1.0.8.11827", "0x4EE5D14"}, // Title Update 1.04.5
                    {"1.0.17.38838", "0x4F78B34"}, // Steam Release
                    {"1.0.17.39540", "0x4F79B34"}, // Epic Release
                    {"1.0.112.6680", "0x5048488"}, // Title Update 1.112
                    {"1.0.113.21181", "0x5048488"}, // Steam Only 1.113 Update
                }
            },
            {
                "LibertyCity", new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    {"1.0.0.14296", "0x4E19888"}, // Base Version 1.0
                    {"1.0.0.14377", "0x4E1D088"}, // Title Update 1.01
                    {"1.0.0.14718", "0x4E33408"}, // Title Update 1.03
                    {"1.0.0.15284", "0x4D4C4B8"}, // Title Update 1.04
                    {"1.0.8.11827", "0x4DC37B8"}, // Title Update 1.04.5
                    {"1.0.17.38838", "0x4F11248"}, // Steam Release
                    {"1.0.17.39540", "0x4F15248"}, // Epic Release
                    {"1.0.112.6680", "0x4FD07BC"}, // Title Update 1.112
                    {"1.0.113.21181", "0x4FD07BC"}, // Steam Only 1.113 Update
                }
            }
        };

        private readonly List<GameInfo> _supportedGames = new()
        {
            new GameInfo { ProcessName = "SanAndreas", DisplayName = "GTA: San Andreas DE" },
            new GameInfo { ProcessName = "ViceCity", DisplayName = "GTA: Vice City DE" },
            new GameInfo { ProcessName = "LibertyCity", DisplayName = "GTA III DE" }
        };

        private readonly string _customPath;
        private readonly string _hotkeyPath;
        private Dictionary<string, Dictionary<string, string>> _customOffsets = new(StringComparer.OrdinalIgnoreCase);
        private HotkeyInfo _currentHotkey;

        public GameAddressConfig(string folder)
        {
            _customPath = Path.Combine(folder, "additional_addresses.json");
            _hotkeyPath = Path.Combine(folder, "hotkey_config.json");
            LoadCustom();
            LoadHotkey();
        }

        private void LoadCustom()
        {
            _customOffsets.Clear();
            if (File.Exists(_customPath))
            {
                try
                {
                    var json = File.ReadAllText(_customPath);
                    _customOffsets = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(json)
                                  ?? new(StringComparer.OrdinalIgnoreCase);
                }
                catch
                {
                    _customOffsets = new(StringComparer.OrdinalIgnoreCase);
                }
            }
        }

        private void LoadHotkey()
        {
            _currentHotkey = new HotkeyInfo { VirtualKey = 0x75, Modifiers = 0, DisplayName = "F6" }; // Default F6

            if (File.Exists(_hotkeyPath))
            {
                try
                {
                    var json = File.ReadAllText(_hotkeyPath);
                    _currentHotkey = JsonSerializer.Deserialize<HotkeyInfo>(json) ?? _currentHotkey;
                }
                catch { }
            }
        }

        public HotkeyInfo GetHotkey() => _currentHotkey;

        public void SaveHotkey(HotkeyInfo hotkey)
        {
            _currentHotkey = hotkey;
            var opts = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(_hotkeyPath, JsonSerializer.Serialize(hotkey, opts));
        }

        public List<GameInfo> GetSupportedGames() => _supportedGames;

        public bool TryGetOffset(string gameName, string? version, out long offset)
        {
            offset = 0;
            if (string.IsNullOrWhiteSpace(version)) return false;

            // Try hardcoded offsets for this game
            if (_hardcodedOffsets.TryGetValue(gameName, out var hardcoded))
            {
                if (hardcoded.TryGetValue(version, out var raw) && TryParseHex(raw, out offset))
                    return true;

                // Try prefix match in hardcoded
                var prefix = string.Join('.', version.Split('.').Take(3));
                var kv = hardcoded.FirstOrDefault(k => k.Key.StartsWith(prefix + ".", StringComparison.OrdinalIgnoreCase));
                if (!kv.Equals(default(KeyValuePair<string, string>)) && TryParseHex(kv.Value, out offset))
                    return true;
            }

            // Try custom offsets for this game
            if (_customOffsets.TryGetValue(gameName, out var custom))
            {
                if (custom.TryGetValue(version, out var raw) && TryParseHex(raw, out offset))
                    return true;

                // Try prefix match in custom
                var prefix = string.Join('.', version.Split('.').Take(3));
                var kv = custom.FirstOrDefault(k => k.Key.StartsWith(prefix + ".", StringComparison.OrdinalIgnoreCase));
                if (!kv.Equals(default(KeyValuePair<string, string>)) && TryParseHex(kv.Value, out offset))
                    return true;
            }

            return false;
        }

        public bool AddAdditionalVersion(string gameName, string version, string hexAddr)
        {
            if (string.IsNullOrWhiteSpace(gameName) || string.IsNullOrWhiteSpace(version) || string.IsNullOrWhiteSpace(hexAddr))
                return false;

            LoadCustom(); // reload current file

            if (!_customOffsets.ContainsKey(gameName))
                _customOffsets[gameName] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (_customOffsets[gameName].ContainsKey(version))
                return false;

            // Strip '0x' prefix if present before storing
            string cleanedHexAddr = hexAddr.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                ? hexAddr.Substring(2)
                : hexAddr;

            _customOffsets[gameName][version] = cleanedHexAddr;
            var opts = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(_customPath, JsonSerializer.Serialize(_customOffsets, opts));

            return true;
        }

        private static bool TryParseHex(string raw, out long value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(raw)) return false;

            raw = raw.Trim();
            // Remove '0x' prefix if present
            if (raw.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                raw = raw.Substring(2);

            return long.TryParse(raw, System.Globalization.NumberStyles.HexNumber, null, out value);
        }
    }

    internal static class Native
    {
        [Flags]
        public enum ProcessAccessFlags : uint
        {
            All = 0x1F0FFF
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(ProcessAccessFlags access, bool inherit, int pid);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr h);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr addr, [Out] byte[] buffer, int size, out IntPtr read);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr addr, byte[] buffer, int size, out IntPtr written);

        [DllImport("user32.dll")] public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
        [DllImport("user32.dll")] public static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    }
}