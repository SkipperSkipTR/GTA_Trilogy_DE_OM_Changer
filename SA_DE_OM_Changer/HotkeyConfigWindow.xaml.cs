using System;
using System.Windows;
using System.Windows.Input;

namespace SA_DE_OM_Changer
{
    public partial class HotkeyConfigWindow : Window
    {
        public HotkeyInfo SelectedHotkey { get; private set; }
        private bool _keyPressed = false;

        public HotkeyConfigWindow(HotkeyInfo currentHotkey)
        {
            InitializeComponent();
            SelectedHotkey = currentHotkey;
            CurrentHotkeyText.Text = $"Current: {currentHotkey.DisplayName}";
            InstructionText.Text = "Press any key combination to set as the new hotkey...";

            // Focus the window so it can capture key presses
            Loaded += (s, e) => Focus();
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);

            // Prevent the window from closing on Alt+F4, Escape, etc during capture
            e.Handled = true;

            // Get the actual key (not modifier keys)
            Key key = e.Key == Key.System ? e.SystemKey : e.Key;

            // Ignore pure modifier presses
            if (key == Key.LeftCtrl || key == Key.RightCtrl ||
                key == Key.LeftAlt || key == Key.RightAlt ||
                key == Key.LeftShift || key == Key.RightShift ||
                key == Key.LWin || key == Key.RWin)
            {
                return;
            }

            // Get modifiers
            uint modifiers = 0;
            if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
                modifiers |= 2; // MOD_CONTROL
            if ((Keyboard.Modifiers & ModifierKeys.Alt) != 0)
                modifiers |= 1; // MOD_ALT
            if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0)
                modifiers |= 4; // MOD_SHIFT

            // Convert WPF Key to Virtual Key Code
            uint virtualKey = (uint)KeyInterop.VirtualKeyFromKey(key);

            // Create display name
            string displayName = GetDisplayName(key, modifiers);

            // Update the display - show both what was pressed and what will be set
            _keyPressed = true;
            PressedKeyText.Text = displayName;
            PressedKeyText.Foreground = System.Windows.Media.Brushes.LightGreen;
            NewHotkeyText.Text = $"New hotkey will be: {displayName}";
            NewHotkeyText.Foreground = System.Windows.Media.Brushes.White;
            InstructionText.Text = "Press OK to confirm, or press another key to change...";

            // Store the captured hotkey
            SelectedHotkey = new HotkeyInfo
            {
                VirtualKey = virtualKey,
                Modifiers = modifiers,
                DisplayName = displayName
            };
        }

        private string GetDisplayName(Key key, uint modifiers)
        {
            string keyName = GetKeyName(key);
            string prefix = "";

            if ((modifiers & 2) != 0) // Ctrl
                prefix += "Ctrl+";
            if ((modifiers & 1) != 0) // Alt
                prefix += "Alt+";
            if ((modifiers & 4) != 0) // Shift
                prefix += "Shift+";

            return prefix + keyName;
        }

        private string GetKeyName(Key key)
        {
            // Handle special keys
            switch (key)
            {
                case Key.D0: return "0";
                case Key.D1: return "1";
                case Key.D2: return "2";
                case Key.D3: return "3";
                case Key.D4: return "4";
                case Key.D5: return "5";
                case Key.D6: return "6";
                case Key.D7: return "7";
                case Key.D8: return "8";
                case Key.D9: return "9";
                case Key.NumPad0: return "Numpad 0";
                case Key.NumPad1: return "Numpad 1";
                case Key.NumPad2: return "Numpad 2";
                case Key.NumPad3: return "Numpad 3";
                case Key.NumPad4: return "Numpad 4";
                case Key.NumPad5: return "Numpad 5";
                case Key.NumPad6: return "Numpad 6";
                case Key.NumPad7: return "Numpad 7";
                case Key.NumPad8: return "Numpad 8";
                case Key.NumPad9: return "Numpad 9";
                case Key.Oem3: return "`";
                case Key.OemMinus: return "-";
                case Key.OemPlus: return "=";
                case Key.OemOpenBrackets: return "[";
                case Key.OemCloseBrackets: return "]";
                case Key.OemPipe: return "\\";
                case Key.OemSemicolon: return ";";
                case Key.OemQuotes: return "'";
                case Key.OemComma: return ",";
                case Key.OemPeriod: return ".";
                case Key.OemQuestion: return "/";
                case Key.Space: return "Space";
                default: return key.ToString();
            }
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            if (!_keyPressed)
            {
                MessageBox.Show("Please press a key combination first.", "No Key Pressed",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}