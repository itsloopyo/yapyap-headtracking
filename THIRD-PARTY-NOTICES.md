# Third-Party Notices

YAPYAP Head Tracking is MIT licensed (see `LICENSE`). It uses or bundles the following third-party software; each component remains under its own license.

## BepInEx

- **Version:** 5.4.23.5
- **License:** LGPL-2.1
- **Upstream:** https://github.com/BepInEx/BepInEx
- **Usage:** Unity plugin loader. `install.cmd` extracts the bundled archive into the game folder if BepInEx is not already present.
- **Bundled:** yes; bundled in the release ZIP and used as the install-time source. Source code is available from the upstream repository per LGPL section 6. The binary is not modified.

---

## HarmonyX

- **Version:** shipped with BepInEx 5.4.23.5 (`0Harmony.dll`)
- **License:** MIT
- **Upstream:** https://github.com/BepInEx/HarmonyX
- **Usage:** Runtime method patching available to the shared CameraUnlock core.
- **Bundled:** yes; ships inside the bundled BepInEx archive.

---

## OpenTrack

- **Version:** protocol only (no code bundled)
- **License:** ISC
- **Upstream:** https://github.com/opentrack/opentrack
- **Usage:** Head pose source. The mod listens for OpenTrack's UDP packet format on port 4242. No OpenTrack code is bundled.
- **Bundled:** no.
