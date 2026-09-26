# YAPYAP Head Tracking

![YAPYAP running with this mod](https://raw.githubusercontent.com/itsloopyo/yapyap-headtracking/main/assets/readme-clip.gif)

An unofficial head tracking mod for YAPYAP that moves the view with your head while your mouse or controller keeps aiming, driven by a webcam, phone, or any OpenTrack compatible tracker, with no VR headset required.

> **Settings have moved.** This version keeps its settings in `BepInEx\config\CameraUnlock.ini`.
> The first time it starts it reads your settings from the old
> `BepInEx\config\com.cameraunlock.yapyap.headtracking.cfg` into the new file, and leaves the old
> file as it was. BepInEx's ConfigurationManager no longer lists the settings: edit
> `CameraUnlock.ini` with any text editor. [Configuration](#configuration) has the details.

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

The next press returns to full tracking.

`Page Down` / `Ctrl+Shift+H` switches between horizon-locked yaw (the default)
and yaw around the camera's current up-axis.

The tracking mode and the yaw mode you pick are saved to `CameraUnlock.ini` and are what the next
start begins with. `End` turns head tracking on and off for this session only; whether it is on at
the next start is the `EnableOnStartup` setting.

These are the default keys. Each action reads a list of keys from `CameraUnlock.ini`
(`ToggleKey`, `CycleTrackingModeKey`, `YawModeKey`), and any key in the list fires it, so you can
add, rebind or remove any of them, the chords included.

The game's crosshair follows your aim while head tracking turns the view. It has no setting.

There is no recenter key in the mod. Centre in your tracker app.

## Configuration

<!-- cameraunlock:config -->
The mod reads its settings from `BepInEx\config\CameraUnlock.ini` in the game folder, and creates the file when it starts and finds none. Edit it with any text editor.

A setting set to `default` takes its value from `Defaults.ini`, which every head tracking mod that keeps its settings in `CameraUnlock.ini` reads. Head tracking mods that keep their settings in another file do not read it, and neither do earlier versions of this mod. Writing a value in place of `default` changes that setting for this game only. When the mod saves a setting that a hotkey changed in game, it writes the new value in place of `default`, so that setting no longer follows `Defaults.ini` in this game until you set it to `default` again.

`Defaults.ini` is `%AppData%\CameraUnlock\Defaults.ini` on Windows; `$XDG_CONFIG_HOME/CameraUnlock/Defaults.ini` on Linux, or `~/.config/CameraUnlock/Defaults.ini` where `XDG_CONFIG_HOME` is not set, under Wine and Proton too; and `~/Library/Application Support/CameraUnlock/Defaults.ini` on macOS. The mod's log, where it writes one, names the file it read.

When the mod starts and finds no `Defaults.ini`, it creates one holding the built-in values, unless Windows runs the game as a packaged app, or the game runs on Linux or macOS without Wine or Proton. The mod never changes `Defaults.ini` after that. Edit it with any text editor.

Earlier versions of the mod kept these settings in `com.cameraunlock.yapyap.headtracking.cfg`, in the same folder. The first time this version starts and finds no `CameraUnlock.ini`, it reads your settings from `com.cameraunlock.yapyap.headtracking.cfg` and writes them into `CameraUnlock.ini`. It never changes `com.cameraunlock.yapyap.headtracking.cfg`, and does not read it again while `CameraUnlock.ini` exists.

A setting that the defaults below set to `default` is written as `default` when the value imported for it equals its default at that start, which is the value `Defaults.ini` gives it, or the built-in value where `Defaults.ini` gives none. It then follows `Defaults.ini`. Every other setting is written with the value imported for it. `RotationEnabled` and `PositionEnabled` are one setting here, the tracking mode, so both are written as `default` or neither is.

Comments, and keys the mod never read, are not carried over. Nor are these, where your old file had them:

- Reticle settings, and a key that toggled the reticle.
- A sensitivity, scale, deadzone, response curve or axis inversion you changed from its default. Set these in your tracker instead.
- The setting for a feature that earlier versions shipped switched off while it was untested. It now follows the mod's default.

An older version of the mod reads `com.cameraunlock.yapyap.headtracking.cfg` and never reads `CameraUnlock.ini`, so a setting you change after updating is not in `com.cameraunlock.yapyap.headtracking.cfg`.

Deleting only `CameraUnlock.ini` makes the next start read `com.cameraunlock.yapyap.headtracking.cfg` again. To go back to the defaults, replace everything in `CameraUnlock.ini` with the defaults below. Every setting they set to `default` then follows `Defaults.ini`.

On Linux and macOS without Wine or Proton, this version reads its settings and saves none: it creates no `CameraUnlock.ini`, reads your settings from `com.cameraunlock.yapyap.headtracking.cfg` again at every start while there is no `CameraUnlock.ini`, and a change made in game lasts until the game closes.

BepInEx's ConfigurationManager no longer lists these settings.

The built-in value of each setting set to `default` below:

- `UdpPort=4242`
- `EnableOnStartup=true`
- `WorldSpaceYaw=true`
- `RotationEnabled=true`
- `LocalSmoothing=0.0`
- `RemoteSmoothing=0.15`
- `PositionEnabled=true`
- `PositionLimitX=0.3`
- `PositionLimitY=0.2`
- `PositionLimitYDown=0.2`
- `PositionLimitZ=0.4`
- `PositionLimitZBack=0.1`
- `TrackerPivotForward=0.0`
- `ToggleKey=End, Ctrl+Shift+Y`
- `CycleTrackingModeKey=PageUp, Ctrl+Shift+G`
- `YawModeKey=PageDown, Ctrl+Shift+H`

With every setting at its default, the file reads:

```ini
; YAPYAP head tracking settings.
; Comments start with ; and go on their own line. Text after a value is part of the value.
; Hotkeys are key names such as End, PageUp or Ctrl+Shift+Y. Separate several with commas; leave empty for none.
; A setting set to default takes its value from Defaults.ini, which every head tracking mod
; that keeps its settings in CameraUnlock.ini reads: %AppData%\CameraUnlock\Defaults.ini on
; Windows, $XDG_CONFIG_HOME/CameraUnlock/Defaults.ini (normally ~/.config/CameraUnlock) on
; Linux, under Wine and Proton too, and ~/Library/Application Support/CameraUnlock/Defaults.ini
; on macOS. The log names the file it read. Write a value instead of default to change that
; setting for this game only.

[CameraUnlock]
; Written by the mod. Leave this section in place.
ConfigFormat=1

[Network]
; UDP port the mod receives tracker data on (OpenTrack protocol).
UdpPort=default

[General]
; true: head tracking is on when the game starts. ToggleKey turns it on and off.
EnableOnStartup=default
; true: yaw turns around the world's up axis. false: around the camera's own up axis.
WorldSpaceYaw=default
; true: turning your head turns the view.
; Tracking mode at startup, with PositionEnabled. The mode hotkey changes both.
RotationEnabled=default

[Smoothing]
; Smoothing when the tracker runs on this PC. 0 is the least, 1 the most.
LocalSmoothing=default
; Smoothing when the tracker is another device on the network, such as a phone.
; 0 is the least, 1 the most.
RemoteSmoothing=default

[Position]
; true: moving your head moves the view.
; Tracking mode at startup, with RotationEnabled. The mode hotkey changes both.
PositionEnabled=default
; How far, in metres, leaning left or right can move the view.
PositionLimitX=default
; How far, in metres, raising your head can move the view.
PositionLimitY=default
; How far, in metres, lowering your head can move the view.
PositionLimitYDown=default
; How far, in metres, leaning forward can move the view.
PositionLimitZ=default
; How far, in metres, leaning back can move the view.
PositionLimitZBack=default
; Metres from the pivot of your neck forward to the point the tracker follows.
; Used to remove the lean that turning your head adds. 0 turns it off.
TrackerPivotForward=default

[Hotkeys]
; Turns head tracking on and off.
ToggleKey=default
; Changes the tracking mode: rotation and position, rotation only, position only.
CycleTrackingModeKey=default
; Switches yaw between the world's up axis and the camera's own (WorldSpaceYaw).
YawModeKey=default

[Notifications]
; true: show whether head tracking is on, and its hotkeys, when the game starts.
ShowStartupNotification=true
; true: show a message when tracker data starts or stops arriving.
ShowConnectionNotifications=true
```
<!-- /cameraunlock:config -->

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

- Close the game, edit `BepInEx\config\CameraUnlock.ini`, then relaunch. Editing the old `.cfg` changes nothing once `CameraUnlock.ini` exists.
- Make sure nothing follows the value on the line: text after a value is part of the value. `YAPYAP\BepInEx\LogOutput.log` names each line the mod could not read and the value it used instead.

**Jittery / unstable tracking**

- Increase OpenTrack smoothing, or the mod's `RemoteSmoothing` (phone/network tracker) or `LocalSmoothing` (tracker on this PC) setting.
- Use a stable webcam, phone mount, or headset connection.

**Wrong rotation axis / yaw feels wrong when looking up or down at extreme angles**

- Toggle between world-locked and camera-local yaw with `Page Down` or `Ctrl+Shift+H`. World-locked (default) keeps yaw horizon-stable no matter where you are pitched; camera-local follows the camera's current up-axis.
- Centre your view in your tracker app (OpenTrack's **Center** bind or Headcam's **CENTER** button).
- If pitch feels inverted, invert it in OpenTrack's output mapping. The mod has no invert or sensitivity settings; the axis corrections it needs are applied internally.

**Known limitations**

- Positional lean is limited by the configured distances but does not check walls. If leaning clips the view through geometry, reduce the position limits or select rotation-only mode with `Page Up` / `Ctrl+Shift+G`.
- Crosshair compensation accounts for head rotation, not parallax from positional lean. Use rotation-only mode if that offset is distracting.

## Updating

For a standalone installation, download the new installer ZIP, extract it, and
run its `install.cmd`. Your `CameraUnlock.ini` is preserved. For a manual installation,
replace the three plugin DLLs with those from the new release and keep your config files.

## Uninstalling

For a standalone installation, run `uninstall.cmd`. This removes the mod DLLs and
leaves `CameraUnlock.ini` and the old `.cfg` in place. BepInEx is only removed if the installer put it there; `/force` also removes a
pre-existing BepInEx installation. Removing BepInEx removes its plugins and config
folder too. If other mods use it, use the manual removal steps below.

For a manual uninstall, remove `YapyapHeadTracking.dll`, `CameraUnlock.Core.dll`,
and `CameraUnlock.Core.Unity.dll` from `YAPYAP\BepInEx\plugins\`. Keep the shared
CameraUnlock DLLs if another mod uses them. You can also delete
`YAPYAP\BepInEx\config\CameraUnlock.ini` and
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
