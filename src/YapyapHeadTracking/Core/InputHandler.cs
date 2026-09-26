using System;
using CameraUnlock.Core.Unity.Extensions;
using YapyapHeadTracking.Config;
using UnityEngine;

namespace YapyapHeadTracking.Core
{
    internal class InputHandler
    {
        private readonly ModConfig _config;

        public event Action OnTogglePressed;
        public event Action OnCycleTrackingModePressed;
        public event Action OnToggleYawModePressed;

        public KeyCode ToggleKey => _config.ToggleKey;
        public KeyCode CycleTrackingModeKey => _config.CycleTrackingModeKey;
        public KeyCode YawModeKey => _config.YawModeKey;

        public InputHandler(ModConfig config)
        {
            _config = config;
        }

        public void CheckInput()
        {
            // Common case: nothing pressed this frame. Skip the GetKeyDown probes and
            // config reads. Holding keys without a fresh down-edge also skips,
            // matching Dispatch's GetKeyDown semantics.
            if (!Input.anyKeyDown)
                return;

            Dispatch(_config.ToggleKey, ChordHotkeys.ToggleLetter, OnTogglePressed);
            Dispatch(_config.CycleTrackingModeKey, ChordHotkeys.PositionLetter, OnCycleTrackingModePressed);
            Dispatch(_config.YawModeKey, ChordHotkeys.FourthToggleLetter, OnToggleYawModePressed);
        }

        private static void Dispatch(KeyCode primary, KeyCode chordLetter, Action handler)
        {
            if (ChordHotkeys.IsActionPressed(primary, chordLetter))
                handler?.Invoke();
        }
    }
}
