# Changelog

## [0.0.1] - 2026-08-18

### Added

- follow core's split of SmoothingFactor into a per-connection pair

### Fixed

- match stub member kinds to the shipped Unity assemblies
- compile the uGUI stubs into UnityEngine.UI, not UnityEngine

All notable changes to this project are documented here. The format follows
[Keep a Changelog](https://keepachangelog.com/), and the project adheres to
[Semantic Versioning](https://semver.org/).

## [Unreleased]

Pre-release development. Dev builds are published to the rolling `dev`
GitHub pre-release; no versioned release has been cut yet.

### Added
- View-matrix head tracking for YAPYAP via BepInEx, built on CameraUnlock.Core.
- Decoupled look and aim: head moves the view, mouse/controller still aims.
  Spell aim (Cinemachine state) and the first-person body IK both read the
  clean camera rotation.
- 6DOF support with cycleable tracking modes (rotation, rotation + position).
- World-locked and camera-local yaw modes.
- Crosshair compensation that follows the clean aim point.
- Game-state detection that suppresses tracking outside gameplay.
- OpenTrack UDP receiver (port 4242) with smoothing, interpolation, and
  auto-recenter on first connection.
- Hotkeys: nav-cluster keys plus Ctrl+Shift chord alternatives for toggle,
  recenter, cycle tracking mode, and yaw mode.

### Changed
- Replaced the `Smoothing` and `PositionSmoothing` config keys with
  `LocalSmoothing` (default 0.0) and `RemoteSmoothing` (default 0.15). The value
  is selected per connection from the packet source address and covers both
  rotation and position.
- Removed the hidden 0.15 baseline smoothing floor, so a tracker running on this
  PC now gets zero-latency tracking by default.
