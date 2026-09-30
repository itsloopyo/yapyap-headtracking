# Changelog

All notable changes to this project are documented here. The format follows
[Keep a Changelog](https://keepachangelog.com/), and the project adheres to
[Semantic Versioning](https://semver.org/).

## [0.3.0] - 2026-09-30

### Changed

- Settings move to `BepInEx\config\CameraUnlock.ini`. Earlier versions of the mod kept these settings in `com.cameraunlock.yapyap.headtracking.cfg`, in the same folder. The first time this version starts and finds no `CameraUnlock.ini`, it reads your settings from `com.cameraunlock.yapyap.headtracking.cfg` and writes them into `CameraUnlock.ini`. It never changes `com.cameraunlock.yapyap.headtracking.cfg`, and does not read it again while `CameraUnlock.ini` exists.
- A setting that the defaults the README shows set to `default` is written as `default` when you never changed it from the default earlier versions used, because `com.cameraunlock.yapyap.headtracking.cfg` does not hold it or holds that default. It then follows `Defaults.ini`, so it takes the value `Defaults.ini` gives it, or the built-in value where `Defaults.ini` gives none, which can differ from the default earlier versions used. A setting you changed is written with the value imported for it, or as `default` where that value equals its default at that start.
- `RotationEnabled` and `PositionEnabled` are one setting here, the tracking mode, so both are written as `default` or neither is.
- Comments, and keys the mod never read, are not carried over. Nor are these, where your old file had them:
  - A sensitivity, scale, deadzone, response curve or axis inversion you changed from its default. Set these in your tracker instead.
  - Reticle settings, and a key that toggled the reticle.
  - A neck pivot distance you changed from its default. The neck pivot is not a setting now.
  - A hotkey set to Ctrl, Shift or Alt on its own. That key goes down before the key of any chord made with it, so the hotkey is left unbound, and it keeps its Ctrl+Shift chord where it has one.
  - A hotkey set to a number that is not a key code Unity names (for example `ToggleKey = 999`). The hotkey is left unbound, the log says so, and it keeps its Ctrl+Shift chord.
- An older version of the mod reads `com.cameraunlock.yapyap.headtracking.cfg` and never reads `CameraUnlock.ini`, so a setting you change after updating is not in `com.cameraunlock.yapyap.headtracking.cfg`.
- Deleting only `CameraUnlock.ini` makes the next start read `com.cameraunlock.yapyap.headtracking.cfg` again. To go back to the defaults, replace everything in `CameraUnlock.ini` with the defaults the README shows. Every setting they set to `default` then follows `Defaults.ini`.
- BepInEx's ConfigurationManager no longer lists these settings. Edit `BepInEx\config\CameraUnlock.ini` with any text editor.
- Hotkeys are written as key names, and each hotkey lists every key that triggers it, the Ctrl+Shift chord included: `ToggleKey=End, Ctrl+Shift+Y`.
- A hotkey bound to a plain key no longer fires while Ctrl and Shift are both held, so Ctrl+Shift with that key reaches only a binding that names the chord.
- On Linux and macOS without Wine or Proton, this version reads its settings and saves none: it creates no `CameraUnlock.ini`, reads your settings from `com.cameraunlock.yapyap.headtracking.cfg` again at every start while there is no `CameraUnlock.ini`, and a change made in game lasts until the game closes.
- The tracking mode (`Page Up`) and the yaw mode (`Page Down`) you pick are saved to `CameraUnlock.ini` and are what the next start begins with. Earlier versions started every session from the file's settings. `End` still changes the current session only.
- The mod keeps no centre of its own. The recenter hotkey (`Home` /
  `Ctrl+Shift+T`), its `Keybindings/RecenterKey` config entry and the recenter
  notification are gone. The tracker app owns centring, so a mod-side centre
  sat in series with the tracker's and the two drifted apart. Centre in your
  tracker app instead: OpenTrack's Center bind, or the CENTER button in
  Headcam.
- Replaced the `Smoothing` and `PositionSmoothing` config keys with
  `LocalSmoothing` (default 0.0) and `RemoteSmoothing` (default 0.15). The value
  is selected per connection from the packet source address and covers both
  rotation and position.
- Removed the hidden 0.15 baseline smoothing floor, so a tracker running on this
  PC now gets zero-latency tracking by default.

### Added

- A setting set to `default` in `CameraUnlock.ini` takes its value from `Defaults.ini`, which every head tracking mod that keeps its settings in `CameraUnlock.ini` reads. Head tracking mods that keep their settings in another file do not read it, and neither do earlier versions of this mod. Writing a value in place of `default` changes that setting for this game only. When the mod saves a setting that a hotkey changed in game, it writes the new value in place of `default`, so that setting no longer follows `Defaults.ini` in this game until you set it to `default` again.
- `Defaults.ini` is `%AppData%\CameraUnlock\Defaults.ini` on Windows; `$XDG_CONFIG_HOME/CameraUnlock/Defaults.ini` on Linux, or `~/.config/CameraUnlock/Defaults.ini` where `XDG_CONFIG_HOME` is not set, under Wine and Proton too; and `~/Library/Application Support/CameraUnlock/Defaults.ini` on macOS. The mod's log, where it writes one, names the file it read.
- When the mod starts and finds no `Defaults.ini`, it creates one holding the built-in values, unless Windows runs the game as a packaged app, or the game runs on Linux or macOS without Wine or Proton. The mod never changes `Defaults.ini` after that.
- View-matrix head tracking for YAPYAP via BepInEx, built on CameraUnlock.Core.
- Decoupled look and aim: head moves the view, mouse/controller still aims.
  Spell aim (Cinemachine state) and the first-person body IK both read the
  clean camera rotation.
- 6DOF support with cycleable tracking modes (rotation, rotation + position).
- World-locked and camera-local yaw modes.
- Crosshair compensation that follows the clean aim point.
- Game-state detection that suppresses tracking outside gameplay.
- OpenTrack UDP receiver (port 4242) with smoothing and interpolation.
- Hotkeys: nav-cluster keys plus Ctrl+Shift chord alternatives for toggle,
  cycle tracking mode, and yaw mode.

### Removed

- `CompensateCrosshair`. The game's crosshair always follows the aim while head tracking turns the view; an imported `CompensateCrosshair=false` is dropped and logged.
- The sensitivity, scale, deadzone, response curve and axis inversion settings (`YawSensitivity`, `PitchSensitivity`, `RollSensitivity`, `PositionSensitivityX`, `PositionSensitivityY`, `PositionSensitivityZ`). Set these in your tracker app instead.
- With these settings at their shipped defaults the camera moves as it did before.
- `TrackerPivotForward`, the neck pivot setting. A distance you changed from the default is not carried over. The mod keeps the 0.08 m every earlier version shipped.

### Fixed

- `OpenTrack connection established` is now written to `BepInEx/LogOutput.log`
  whether or not the on-screen connection popup is enabled. It used to sit
  inside the `ShowConnectionNotifications` gate, so a user who turned the popup
  off also lost the only evidence in the log that tracker packets ever arrived.
  The line now also names the UDP port and whether the sender is remote.

## [0.2.0] - 2026-08-20

### Changed

- Maintenance release (no user-facing changes).

## [0.1.0] - 2026-08-20

### Fixed

- give the forward lean its own travel budget again
- stop keeping a mod-side centre, the tracker app owns centring

## [0.0.2] - 2026-08-18

### Changed

- Maintenance release (no user-facing changes).

## [0.0.1] - 2026-08-18

### Added

- follow core's split of SmoothingFactor into a per-connection pair

### Fixed

- match stub member kinds to the shipped Unity assemblies
- compile the uGUI stubs into UnityEngine.UI, not UnityEngine
