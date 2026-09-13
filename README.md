# YAPYAP Head Tracking

![YAPYAP running with this mod](https://raw.githubusercontent.com/itsloopyo/yapyap-headtracking/main/assets/readme-clip.gif)

An unofficial head tracking mod for YAPYAP that moves the view with your head while your mouse or controller keeps aiming, driven by a webcam, phone, or any OpenTrack compatible tracker, with no VR headset required.

## Features

- **Decoupled look and aim** - head movement rotates the view while your normal controls keep aiming.
- **6DOF tracking** - yaw, pitch and roll plus positional lean, peek and duck.
- **Works with any OpenTrack compatible tracker** - free options available for PC, iOS and Android
- **Crosshair compensation** - the crosshair follows your aim direction as you turn your head.

## Requirements

- [YAPYAP on Steam](https://store.steampowered.com/app/3834090/YAPYAP/).
- [OpenTrack](https://github.com/opentrack/opentrack/releases) or another tracking source that can send OpenTrack UDP data.
- Windows 10/11, 64-bit.
- BepInEx 5 x64, included in the standalone installer and provisioned by Lopari.

## Installation

### Lopari

Download [Lopari](https://lopari.app), choose **YAPYAP**, and click
**Play with head tracking**.

### Standalone Installer

1. Download the ZIP ending in `-installer.zip` from the [latest release](https://github.com/itsloopyo/yapyap-headtracking/releases/latest).
2. Extract it anywhere.
3. Double-click `install.cmd`.
4. Configure your tracker using the setup below.
5. Launch the game.

If the installer cannot find your game, set `YAPYAP_PATH` to the game folder or pass the game folder as the first argument:

```powershell
$env:YAPYAP_PATH = "D:\Games\YAPYAP"
.\install.cmd
.\install.cmd "D:\Games\YAPYAP"
```

### Manual Installation

1. Install [BepInEx 5 x64](https://github.com/BepInEx/BepInEx/releases) into the YAPYAP game folder.
2. Run the game once so BepInEx creates its folders.
3. From the installer ZIP's `plugins\` folder, copy `YapyapHeadTracking.dll`, `CameraUnlock.Core.dll`, and `CameraUnlock.Core.Unity.dll` into `YAPYAP\BepInEx\plugins\`. If you have the Nexus ZIP instead, extract it into the game folder; it already contains the `BepInEx\plugins\` layout.
4. Configure your tracker using the setup below, then launch the game.

After launch, `YAPYAP\BepInEx\LogOutput.log` should contain `YAPYAP Head Tracking initialized`.

## Setting Up OpenTrack

The mod accepts OpenTrack UDP pose data on port `4242`. Use the webcam,
phone, or hardware route below that matches your tracker.

1. Install [OpenTrack](https://github.com/opentrack/opentrack/releases).
2. Pick a tracker under **Input**, using the notes below.
3. Set **Output** to **UDP over network**, host `127.0.0.1`, port `4242`.
4. Press **Start**. Tracking and the game can start in either order.

### Webcam

OpenTrack ships a `neuralnet tracker` input that reads a plain webcam. Select it
under **Input**, pick your camera in its settings, and use the output settings
above. How well it tracks depends on your camera and your lighting, so try it
before buying anything.

### Phone

A phone app can reach the mod directly if it sends OpenTrack UDP pose data.
Point it at this PC's IP address (run `ipconfig` to find it) on port `4242`.
Check that the app's output supports the OpenTrack UDP protocol.
[Headcam](https://headcam.app) is my own free phone tracker; it filters on-device
and can send directly to the mod.

Sending direct works when the app filters its own signal on the device. The
mod has separate local and remote smoothing settings. If the view shakes while
your head is still, point the app at OpenTrack's **UDP over network** *input*
on some other port, say `5252`,
and let OpenTrack's filters and curves clean it up before its output forwards to
`127.0.0.1:4242`.

Anything arriving from outside `127.0.0.0/8` counts as a remote connection and
is smoothed with `RemoteSmoothing` rather than `LocalSmoothing`. That includes a
tracker on this very PC that sends to the machine's own LAN address, because the
mod reads the source address and not the machine.

### VR headset or other hardware

If your device has an OpenTrack input driver, select it under **Input** and use
the same output settings. OpenTrack's own **Input** list is the authority on
what it can read; the mod only ever sees what OpenTrack sends.

### Centring

Centre in your tracker app, using OpenTrack's **Center** bind or Headcam's
**CENTER** button. The mod keeps no centre of its own. If the view sits off to
one side, centre it in the tracker.

## Controls

The Nav-cluster and Chord columns are equivalent. Use whichever your keyboard has.

| Action | Nav-cluster | Chord |
|--------|-------------|-------|
| Toggle tracking | `End` | `Ctrl+Shift+Y` |
| Cycle tracking mode | `Page Up` | `Ctrl+Shift+G` |
| Toggle yaw mode | `Page Down` | `Ctrl+Shift+H` |

`Page Up` / `Ctrl+Shift+G` cycles tracking mode:

1. Full tracking: rotation and position.
2. Rotation only: positional tracking disabled.
3. Position only: rotational tracking disabled.

The next press returns to full tracking. `PositionEnabled = false` starts in
rotation-only mode.

`Page Down` / `Ctrl+Shift+H` switches between horizon-locked yaw (the default)
and yaw around the camera's current up-axis. These toggles last for the current
game session; use the config to set startup behaviour.

There is no recenter key in the mod. Centre in your tracker app.

## Configuration

The config file is created after the first launch at `YAPYAP\BepInEx\config\com.cameraunlock.yapyap.headtracking.cfg`.
Close the game before editing it, then relaunch to apply your changes. These
are the default values:

```ini
[General]
# Whether head tracking is enabled when the game starts.
EnabledOnStartup = true

# Whether to show an on-screen notification when the plugin initializes.
ShowStartupNotification = true

# true = horizon-locked yaw, false = camera-local yaw.
WorldSpaceYaw = true

[UI]
# Whether to show notifications when OpenTrack connection is lost or restored.
ShowConnectionNotifications = true

# Move the game's crosshair to where your aim points while the view is head-rotated.
CompensateCrosshair = true

[Keybindings]
# Key to toggle head tracking on or off.
ToggleKey = End

# Key to cycle full, rotation only, and position only tracking modes.
CycleTrackingModeKey = PageUp

# Key to toggle world-locked vs camera-local yaw.
YawModeKey = PageDown

[Network]
# UDP port to listen for OpenTrack data.
UDPPort = 4242

[Sensitivity]
# Rotation sensitivity multipliers.
YawSensitivity = 1
PitchSensitivity = 1
RollSensitivity = 1

[Smoothing]
# Smoothing when the tracker runs on this machine (loopback). 0 is responsive, 1 is heavy.
LocalSmoothing = 0

# Smoothing when the tracker is a remote device on the network. 0 is responsive, 1 is heavy.
RemoteSmoothing = 0.15

[Position]
# Enable positional tracking for lean and peek movement.
PositionEnabled = true

# Position sensitivity multipliers.
PositionSensitivityX = 1
PositionSensitivityY = 1
PositionSensitivityZ = 1

# Position limits in meters.
PositionLimitX = 0.3
PositionLimitY = 0.2
PositionLimitZ = 0.4
PositionLimitZBack = 0.1

# Distance in meters from the head pivot to the tracker face point.
TrackerPivotForward = 0.08
```

Smoothing covers both rotation and position. Which of the two values applies is
decided from the packet source address: loopback uses `LocalSmoothing`, other
addresses use `RemoteSmoothing`. Changing sender takes effect without restarting
the game. Edit filtering and response curves in your tracker first.

## Troubleshooting

Start with `YAPYAP\BepInEx\LogOutput.log`. The mod logs initialization, the UDP
port, and when a tracker connection is established or lost.

**Mod not loading**

- Confirm all three DLLs listed under Manual Installation are in `YAPYAP\BepInEx\plugins\`.
- For a manual install, check that you installed BepInEx **5 x64**.
- Check `YAPYAP\BepInEx\LogOutput.log` for `YAPYAP Head Tracking`.
- Re-run `install.cmd` if BepInEx folders are missing.

**No tracking response**

- Confirm OpenTrack is running and output is UDP to `127.0.0.1:4242`.
- Check `YAPYAP\BepInEx\LogOutput.log` for `OpenTrack connection established`.
- Press `End` or `Ctrl+Shift+Y` if tracking is disabled.
- For a direct phone connection, check the PC's LAN address, port `4242`, and Windows Firewall access for the game.

**Tracking stops in menus or overlays**

**By design:** tracking is suppressed in menus, settings, chat, and spell wheels,
and when the game loses input focus. Return to gameplay to resume tracking.

**Config changes do not apply**

- Close the game, edit the config at the path above, then relaunch.
- Use `WorldSpaceYaw` and `PositionEnabled` for startup defaults; the hotkeys change the current session.

**Jittery / unstable tracking**

- Increase OpenTrack smoothing, or the mod's `RemoteSmoothing` (phone/network tracker) or `LocalSmoothing` (tracker on this PC) setting.
- Use a stable webcam, phone mount, or headset connection.
- Reduce noisy position input by lowering `PositionSensitivityX`, `PositionSensitivityY`, or `PositionSensitivityZ`.

**Wrong rotation axis / yaw feels wrong when looking up or down at extreme angles**

- Toggle between world-locked and camera-local yaw with `Page Down` or `Ctrl+Shift+H`. World-locked (default) keeps yaw horizon-stable no matter where you are pitched; camera-local follows the camera's current up-axis.
- Centre your view in your tracker app (OpenTrack's **Center** bind or Headcam's **CENTER** button).
- If pitch feels inverted, check your OpenTrack input mapping before changing mod sensitivity.

**Known limitations**

- Positional lean is limited by the configured distances but does not check walls. If leaning clips the view through geometry, reduce the position limits or select rotation-only mode with `Page Up` / `Ctrl+Shift+G`.
- Crosshair compensation accounts for head rotation, not parallax from positional lean. Use rotation-only mode if that offset is distracting.

## Updating

For a standalone installation, download the new installer ZIP, extract it, and
run its `install.cmd`. Your config is preserved. For a manual installation,
replace the three plugin DLLs with those from the new release and keep your config.

## Uninstalling

For a standalone installation, run `uninstall.cmd`. This removes the mod DLLs.
BepInEx is only removed if the installer put it there; `/force` also removes a
pre-existing BepInEx installation. Removing BepInEx removes its plugins and config
folder too. If other mods use it, use the manual removal steps below.

For a manual uninstall, remove `YapyapHeadTracking.dll`, `CameraUnlock.Core.dll`,
and `CameraUnlock.Core.Unity.dll` from `YAPYAP\BepInEx\plugins\`. Keep the shared
CameraUnlock DLLs if another mod uses them. You can also delete
`YAPYAP\BepInEx\config\com.cameraunlock.yapyap.headtracking.cfg` to remove your
settings. Keep BepInEx if other mods need it.

## Building from Source

Prerequisites: Windows, .NET SDK 8, Git and [pixi](https://pixi.sh).
`pixi run package` runs setup, restore and build before packaging, using the
same task as CI. Setup compiles the Unity reference assemblies from
the stub sources in the cameraunlock-core submodule and takes the BepInEx
references from the vendored loader archive.

```powershell
git clone --recursive https://github.com/itsloopyo/yapyap-headtracking.git
cd yapyap-headtracking
pixi run package
```

The packages are written to `release/` as
`YapyapHeadTracking-v<version>-installer.zip` and
`YapyapHeadTracking-v<version>-nexus.zip`.

## Community & Support

- Discord: [Loop's Head Tracking Hangout](https://discord.com/invite/dxyZdyFNT9) - setup help, bug reports, and new-release announcements
- [Lopari](https://lopari.app) - free Windows launcher with one-click install and launch for the released head-tracking mods
- [Headcam](https://headcam.app) - free app that turns your iPhone or Android phone into the head tracker

## License

MIT License - see [LICENSE](LICENSE) for details.
Third-party licence texts and attribution are in [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).

## Credits

- [maisonbap](https://store.steampowered.com/app/3834090/YAPYAP/) - developer and publisher of YAPYAP.
- [BepInEx](https://github.com/BepInEx/BepInEx) - Unity plugin framework.
- [OpenTrack](https://github.com/opentrack/opentrack) - head tracking software.
- [CameraUnlock.Core](https://github.com/itsloopyo/cameraunlock-core) - shared head tracking library.

## Disclaimer

This mod is not affiliated with, endorsed by, or supported by maisonbap. Use at your own risk.
