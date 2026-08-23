> [!CAUTION]
> ## Experimental prototype - expect missing core features
>
> This is **not** a finished mod.
>
> Current builds may only test whether head tracking can drive the camera. Bug fixes and core features like decoupled look/aim, independent reticle behavior, correct shot direction, off-screen reticle support, movement handling, and comfort tuning may be missing at this early stage of development.

# YAPYAP Head Tracking

Head tracking for [YAPYAP](https://store.steampowered.com/app/3834090/YAPYAP/) that lets you look around with OpenTrack or a phone tracker while mouse or controller aim stays independent, with no VR required.

<!-- ![Mod GIF](https://raw.githubusercontent.com/itsloopyo/yapyap-headtracking/main/assets/readme-clip.gif) -->

## Features

- **Decoupled look and aim** - head movement rotates the view while your normal controls keep aiming.
- **6DOF positional tracking** - lean, peek, and shift your viewpoint with supported trackers.
- **Parallax-correct crosshair** - the crosshair follows the true aim point while the view is head-rotated.

## Requirements

- [YAPYAP on Steam](https://store.steampowered.com/app/3834090/YAPYAP/).
- [OpenTrack](https://github.com/opentrack/opentrack/releases) or another tracking source that can send OpenTrack UDP data.
- Windows 10/11, 64-bit.

## Installation

1. Download the latest installer ZIP from [Releases](https://github.com/itsloopyo/yapyap-headtracking/releases).
2. Extract it anywhere.
3. Double-click `install.cmd`.
4. Configure OpenTrack to output UDP to `127.0.0.1:4242`.
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
3. Copy `YapyapHeadTracking.dll`, `CameraUnlock.Core.dll`, and `CameraUnlock.Core.Unity.dll` into `YAPYAP\BepInEx\plugins\`.
4. If you downloaded the Nexus ZIP, extract it into the game folder after BepInEx is installed.

## Setting Up OpenTrack

In OpenTrack, set Output to `UDP over network`, set the host to `127.0.0.1`, set the port to `4242`, choose your tracker as Input, then click Start.

### VR Headset Setup

Use Air Link or Virtual Desktop to connect the headset to your PC, start SteamVR, then select SteamVR as the OpenTrack input.

### Webcam Setup

Use OpenTrack's neuralnet tracker input with your webcam, then send output through UDP to `127.0.0.1:4242`.

### Phone App Setup

If your phone app smooths its own data, send directly to your PC on port `4242`. If you want OpenTrack curves or filtering, send the phone app to OpenTrack first, then relay OpenTrack output to `127.0.0.1:4242`.

## Controls

The Nav-cluster and Chord columns are equivalent. Use whichever your keyboard has.

| Action | Nav-cluster | Chord |
|--------|-------------|-------|
| Toggle tracking | `End` | `Ctrl+Shift+Y` |
| Cycle tracking mode | `Page Up` | `Ctrl+Shift+G` |
| Toggle yaw mode | `Page Down` | `Ctrl+Shift+H` |

`Page Up` / `Ctrl+Shift+G` cycles tracking mode:

1. Normal head-tracked gameplay
2. Positional tracking disabled, rotational tracking enabled
3. Rotational tracking disabled, positional tracking enabled
4. Back to normal

## Configuration

The config file is created after the first launch at `YAPYAP\BepInEx\config\com.cameraunlock.yapyap.headtracking.cfg`.

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
decided per connection from the packet source address: a tracker running on this
PC uses `LocalSmoothing`, a phone or other network device uses `RemoteSmoothing`.
Switching between them takes effect without restarting the game.

## Troubleshooting

**Mod not loading**

- Confirm `YapyapHeadTracking.dll` is in `YAPYAP\BepInEx\plugins\`.
- Check `YAPYAP\BepInEx\LogOutput.log` for `YAPYAP Head Tracking`.
- Re-run `install.cmd` if BepInEx folders are missing.

**No tracking response**

- Confirm OpenTrack is running and output is UDP to `127.0.0.1:4242`.
- Check `YAPYAP\BepInEx\LogOutput.log` for `OpenTrack connection established`.
- Tracking applies during gameplay and is suppressed in menus, settings, chat, and spell wheels.

**Jittery / unstable tracking**

- Increase OpenTrack smoothing, or the mod's `RemoteSmoothing` (phone/network tracker) or `LocalSmoothing` (tracker on this PC) setting.
- Use a stable webcam, phone mount, or headset connection.
- Reduce noisy position input by lowering `PositionSensitivityX`, `PositionSensitivityY`, or `PositionSensitivityZ`.

**Wrong rotation axis / yaw feels wrong when looking up or down at extreme angles**

- Toggle between world-locked and camera-local yaw with `Page Down` or `Ctrl+Shift+H`. World-locked (default) keeps yaw horizon-stable no matter where you are pitched; camera-local follows the camera's current up-axis.
- Sit straight and centre your view in your tracker app (OpenTrack's Center bind, or the CENTER button in a phone tracker app). The mod keeps no centre of its own, it applies the pose the tracker sends.
- If pitch feels inverted, check your OpenTrack input mapping before changing mod sensitivity.

## Updating

Download the new release and run `install.cmd` again. Your config is preserved.

## Uninstalling

Run `uninstall.cmd`. This removes the mod DLLs. BepInEx is only removed if the installer put it there. Use `uninstall.cmd /force` to remove it anyway.

## Building from Source

Prerequisites: .NET SDK 8+ and [pixi](https://pixi.sh). No YAPYAP install is
needed to build: `pixi run setup` compiles the Unity reference assemblies from
the checked-in stub sources and takes the BepInEx references from the vendored
loader archive.

```powershell
git clone --recursive https://github.com/itsloopyo/yapyap-headtracking.git
cd yapyap-headtracking
pixi run setup
pixi run build
pixi run package
```

## Community & Support

- Discord: [Loop's Head Tracking Hangout](https://discord.com/invite/dxyZdyFNT9) - setup help, bug reports, and new-release announcements
- [Lopari](https://lopari.app) - free Windows launcher with one-click install and launch for the released head-tracking mods
- [Headcam](https://headcam.app) - free app that turns your iPhone or Android phone into the head tracker

## License

MIT License - see [LICENSE](LICENSE) for details.

## Credits

- [maisonbap](https://store.steampowered.com/app/3834090/YAPYAP/) - developer and publisher of YAPYAP.
- [BepInEx](https://github.com/BepInEx/BepInEx) - Unity plugin framework.
- [OpenTrack](https://github.com/opentrack/opentrack) - head tracking software.
- [CameraUnlock.Core](https://github.com/itsloopyo/cameraunlock-core) - shared head tracking library.

## Disclaimer

This mod is not affiliated with, endorsed by, or supported by maisonbap. Use at your own risk.
