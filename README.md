# GTA Trilogy DE on_mission Changer

A WPF application to toggle the `on_mission` variable in GTA Definitive Edition games using memory manipulation.

## Features

### Supported Games
- **GTA III - The Definitive Edition** (LibertyCity.exe)
- **GTA Vice City - The Definitive Edition** (ViceCity.exe)  
- **GTA San Andreas - The Definitive Edition** (SanAndreas.exe)

The program automatically detects which game is running and attaches to it.

### Configurable Hotkey
- Default: **F6**
- Change via "Change Hotkey" button by pressing any key
- Supports:
  - Function keys (F1-F12)
  - Letter keys (A-Z)
  - Number keys (0-9 and Numpad 0-9)
  - Any key with Ctrl, Alt, or Shift modifiers

### Version Management
Each game maintains its own set of version offsets:
- Hardcoded offsets for versions between 1.0 - 1.113
- Add custom versions via the "Add Version" button
- Automatic version detection from game executable

## File Structure

### Configuration Files
All configuration files are stored in the application directory:

**additional_addresses.json**
```json
{
  "SanAndreas": {
    "1.0.113.21181": "51BF148"
  },
  "ViceCity": {
    "1.0.0.15": "4A0B60"
  }
}
```

**hotkey_config.json**
```json
{
  "VirtualKey": 117,
  "Modifiers": 0,
  "DisplayName": "F6"
}
```

## Usage

1. **Launch the application**
2. **Start one of the supported games**
3. **Wait for "Attached" status** (appears automatically)
4. **Press the configured hotkey** to toggle on_mission value

### Adding a New Version
1. Click "Add Version" button
2. Select the game from dropdown
3. Enter version string (e.g., `1.0.113.21181`)
   - Hover over ⓘ icon for instant tooltip on finding version
4. Enter hex address (e.g., `51BF148` or `0x51BF148`)
   - Hover over ⓘ icon for instant tooltip with Cheat Engine instructions
5. Click OK

**Note**: All tooltips appear instantly on hover with no delay.

### Changing Hotkey
1. Click "Change Hotkey" button
2. Press any key or key combination you want to use
3. The dialog shows what you pressed in real-time
4. Click OK to confirm or press another key to change
5. New hotkey takes effect immediately

## Finding on_mission Offsets

For each game version, you need to find the `on_mission` memory address:

1. **Use Cheat Engine** or similar memory scanner
2. **Search for 4 bytes value** that changes between 0 and 1
3. **Start a mission** → value changes to 1
4. **Complete/fail mission** → value changes to 0
5. **Double click on address**: It would look something like `YourGame.exe + Offset`
6. **Add via "Add Version"** button

## Requirements

- .NET 8.0 or higher
- Windows OS
- Administrator privileges (for memory access)
- One of the supported GTA DE games

## Troubleshooting

**"Unsupported version" error:**
- Use "Add Version" to add your game version
- Find the offset using Cheat Engine

**Hotkey not working:**
- Check if another application is using the same hotkey
- Try a different hotkey combination
- Ensure the application window exists (minimized is OK)

**Failed to attach:**
- Run application as Administrator
- Ensure antivirus isn't blocking it
- Check that the game process name matches exactly
